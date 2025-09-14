using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Archontic.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Weapons.Melee;
using Content.Shared.Projectiles;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Atmos;

using Content.Shared.Inventory.Events;
using Content.Shared.Throwing;
using Content.Shared.Examine;
using Content.Shared.Hands;

using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

using Content.Server.Polymorph.Components;
using Content.Server.Spawners.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Destructible;

using System.Linq;
using System.Text;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedArchonSystem _archonSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly PolymorphSystem _morph = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        /// <summary>
        /// ArchonSystem
        /// </summary>
        SubscribeLocalEvent<StasisArchonOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ArchonDocumentComponent, ComponentShutdown>(OnDocumentDestroy);
        SubscribeLocalEvent<ArchonDataComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ArchonHealthComponent, DamageChangedEvent>(OnDamage);

        /// <summary>
        /// ArchonSystem.Research
        /// </summary>
        SubscribeLocalEvent<ArchonAnalyzerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ArchonScannerComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundEnded);

        /// <summary>
        /// ArchonSystem.Generate
        /// </summary>
        SubscribeLocalEvent<ArchonGenerateComponent, MapInitEvent>(OnMapInit);

        /// <summary>
        /// ArchonSystem.Tests
        /// </summary>
        SubscribeLocalEvent<ArchonDataComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<ArchonDataComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ArchonDataComponent, DamageChangedEvent>(OnDamageTest);
        SubscribeLocalEvent<ArchonDataComponent, ArchonBreachEvent>(OnBreach);
        SubscribeLocalEvent<ArchonDataComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<ArchonDataComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ArchonDataComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ArchonDataComponent, ArchonDeathEvent>(OnDeath);
        SubscribeLocalEvent<ArchonDataComponent, ThrowEvent>(OnThrow);
    }

    private int UpdateSpeed = 2;
    private TimeSpan NextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;

        if (curTime < NextUpdate)
            return;

        NextUpdate = curTime + TimeSpan.FromSeconds(UpdateSpeed);

        var beaconQuery = EntityQueryEnumerator<ArchonBeaconComponent, TransformComponent>();
        var archonQuery = EntityQueryEnumerator<PolymorphedEntityComponent>();

        while (beaconQuery.MoveNext(out var uid, out var beaconComp, out var xform))
        {
            BeaconUpdate(uid, beaconComp, xform);
        }

        while (archonQuery.MoveNext(out var polyUid, out var polyComp))
        {
            if (!TryComp<ArchonDataComponent>(polyComp.Parent, out var dataComp))
                continue;

            ArchonUpdate(polyComp.Parent.Value, dataComp);
        }
    }

    private void BeaconUpdate(EntityUid uid, ArchonBeaconComponent comp, TransformComponent xform)
    {
        if (!_power.IsPowered(uid))
        {
            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.NonPowered);
            return;
        }

        if (!TryComp<ArchonDataComponent>(comp.LinkedArchon, out var dataComp))
        {
            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.None);
            return;
        }

        if (CheckInContainment(uid, comp, dataComp, xform))
        {
            int mod = 1;

            // Кол-во очков модифируется относительно класса архонта
            if (comp.ModificatePointsByClass)
            {
                mod = dataComp.Class switch
                {
                    ArchonClass.Safe => 1,
                    ArchonClass.Keter => 2,
                    ArchonClass.Euclid => 3,
                    ArchonClass.Thaumiel => 4,
                };
            }

            if (!_research.TryGetClientServer(uid, out var server, out var serverComponent))
                return;

            _research.ModifyServerPoints(server.Value, comp.ResearchPointsPerSecond * mod, serverComponent);
        }
    }

    private void ArchonUpdate(EntityUid uid, ArchonDataComponent comp)
    {
        if (_gameTiming.CurTime >= comp.StasisExit)
        {
            if (comp.HaveStates == false || comp.State != ArchonState.Stasis)
                return;

            ForceStasisExit(uid, comp);
        }
    }

    /// <summary>
    /// Перемещение архонта в состояние стазиса
    /// </summary>
    public void ForceStasis(EntityUid uid, ArchonDataComponent comp)
    {
        comp.LastState = ArchonState.Stasis;

        EnsureComp<PolymorphableComponent>(uid);

        // Используем полиморфы, так как это наиболее лёгкий вариант, иначе нужно заморчиваться в заморозками всякими, в котором я не разбираюсь
        comp.PolymorphEntity = _morph.PolymorphEntity(uid, comp.StasisPrototype);
    }

    /// <summary>
    /// Перемещение архонта в обычное состояние
    /// </summary>
    public void ForceStasisExit(EntityUid uid, ArchonDataComponent comp)
    {
        if (!TryComp<PolymorphedEntityComponent>(comp.PolymorphEntity, out var polyComp) || comp.PolymorphEntity == null)
            return;

        comp.LastState = ArchonState.Basic;

        _morph.Revert(comp.PolymorphEntity.Value);
    }

    /// <summary>
    /// Перемещение архонта в состояние пробуждения
    /// </summary>
    public void ForceAwake(EntityUid uid, ArchonDataComponent dataComp)
    {
        dataComp.LastState = ArchonState.Awake;

        if (TryComp<ArchonHealthComponent>(uid, out var healthComp))
            healthComp.Health *= 3;

        if (TryComp<StaminaComponent>(uid, out var staminaComp))
            staminaComp.CritThreshold *= 3;

        SetComponents(uid, dataComp, "Generic", 2, 4);
        SetArchonClass(dataComp);

        if (dataComp.Document is not { } documentUid)
            return;

        if (!TryComp<PaperComponent>(documentUid, out var paperComp) ||
            !TryComp<ArchonDocumentComponent>(documentUid, out var documentComp))
            return;

        if (documentComp.Archon != uid)
            return;

        SetDocumentStatus((uid, dataComp), "[color=#cc0836]Пробуждён[/color]", documentUid, paperComp);
    }

    private void SetDocumentStatus(Entity<ArchonDataComponent> ent, string status, EntityUid documentUid, PaperComponent paperComp)
    {
        var content = paperComp.Content;
        if (!string.IsNullOrEmpty(content))
        {
            content = content.Replace("Статус объекта: Под наблюдением", $"Статус объекта: {status}");
            _paperSystem.SetContent(documentUid, content);
        }
    }

    /// <summary>
    /// Стазис архонта при спавне
    /// </summary>
    private void OnComponentStartup(EntityUid uid, ArchonDataComponent dataComp, ComponentStartup args)
    {
        if (dataComp.HaveStates)
        {
            dataComp.State = ArchonState.Stasis;
            dataComp.StasisExit = _gameTiming.CurTime + dataComp.StasisDelay;

            ForceStasis(uid, dataComp);
        }
    }

    /// <summary>
    /// Действия при получении урона Архонтом, чучут щиткод
    /// </summary>
    private void OnDamage(Entity<ArchonHealthComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta)
            return;

        var totalDamage = delta.GetTotal();
        var comp = ent.Comp;

        if (totalDamage < comp.Health)
            return;

        var dataComp = EnsureComp<ArchonDataComponent>(ent);

        if (dataComp.Destructibility == ArchonDestructibility.Rebirth)
        {
            SetDestructibility(ent, dataComp);
            ForceAwake(ent, dataComp);

            dataComp.State = ArchonState.Awake;
        }
        else
        {

            if (dataComp.Document != null)
                OnArchonDeath((ent.Owner, dataComp));

            QueueDel(ent);

        }
    }

    /// <summary>
    /// При смерти архонта
    /// </summary>
    private void OnArchonDeath(Entity<ArchonDataComponent> ent)
    {
        if (ent.Comp.Document is not { } documentUid)
            return;

        if (!TryComp<PaperComponent>(documentUid, out var paperComp) ||
            !TryComp<ArchonDocumentComponent>(documentUid, out var documentComp))
            return;

        if (documentComp.Archon != ent.Owner)
            return;

        SetDocumentStatus(ent, "[color=#cc0836]Списан[/color]", documentUid, paperComp);

        _paperSystem.TryStamp((documentUid, paperComp), new StampDisplayInfo
        {
            StampedName = "stamp-component-stamped-name-expunged",
            StampedColor = Color.FromHex("#8B0000")
        }, "paper_stamp-expunged");

        if (TryComp<ArchonHealthComponent>(ent, out var healthComp))
        {
            Spawn(healthComp.DeathEffect, Transform(ent).Coordinates);
        }

        RaiseLocalEvent(ent, new ArchonDeathEvent());
    }

    /// <summary>
    /// При попадании ХИД снарядом
    /// </summary>
    private void OnProjectileHit(EntityUid uid, StasisArchonOnCollideComponent comp, ref ProjectileHitEvent args)
    {
        if (!TryComp<ArchonDataComponent>(args.Target, out var dataComp) || dataComp.State == ArchonState.Awake)
            return;

        if (dataComp.StasisHits >= comp.MaxHits)
            return;

        dataComp.StasisHits++;
        dataComp.StasisExit = _gameTiming.CurTime + comp.StasisDelay;

        ForceStasis(args.Target, dataComp);
    }

    /// <summary>
    /// При уничтожении документа
    /// </summary>
    private void OnDocumentDestroy(Entity<ArchonDocumentComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Archon == null)
            return;

        if (!TryComp<ArchonDataComponent>(ent.Comp.Archon.Value, out var dataComp))
            return;

        dataComp.Expunged = true;
    }
}
