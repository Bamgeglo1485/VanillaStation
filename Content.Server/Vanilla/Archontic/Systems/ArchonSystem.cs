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
        SubscribeLocalEvent<ArchonComponent, DamageChangedEvent>(OnDamage);

        /// <summary>
        /// ArchonSystem.Research
        /// </summary>
        SubscribeLocalEvent<ArchonAnalyzerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ArchonScannerComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundEnded);

        /// <summary>
        /// ArchonSystem.Generate
        /// </summary>
        SubscribeLocalEvent<ArchonComponent, MapInitEvent>(OnMapInit);
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

            if (comp.ModificatePointsByClass)
            {
                mod = dataComp.Class switch
                {
                    ArchonClass.Safe => 1,
                    ArchonClass.Euclid => 2,
                    ArchonClass.Keter => 3,
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
        if (comp.HaveStates == false || comp.State != ArchonState.Stasis)
            return;

        if (_gameTiming.CurTime >= comp.StasisExit)
        {
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

        comp.PolymorphEntity = _morph.PolymorphEntity(uid, comp.StasisPrototype);

    }

    /// <summary>
    /// Перемещение архонта в обычное
    /// </summary>
    public void ForceStasisExit(EntityUid uid, ArchonDataComponent comp)
    {

        if (!TryComp<PolymorphedEntityComponent>(comp.PolymorphEntity, out var polyComp) || comp.PolymorphEntity == null)
            return;

        comp.LastState = ArchonState.Basic;

        _morph.Revert(comp.PolymorphEntity.Value);

    }

    private void OnComponentStartup(EntityUid uid, ArchonDataComponent dataComp, ComponentStartup args)
    {
        if (dataComp.HaveStates)
        {
            dataComp.LastState = ArchonState.Stasis;

            dataComp.StasisExit = _gameTiming.CurTime + dataComp.StasisDelay;

            ForceStasis(uid, dataComp);
        }
    }

        /// <summary>
        /// Действия при смерти Архонта, чучут щиткод
        /// </summary>
        private void OnDamage(Entity<ArchonComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta)
            return;

        var totalDamage = delta.GetTotal();
        var comp = ent.Comp;

        if (totalDamage < comp.Health)
            return;

        var dataComp = EntityManager.EnsureComponent<ArchonDataComponent>(ent);

        if (dataComp.Destructibility == ArchonDestructibility.Rebirth)
        {
            SetDestructibility(ent, dataComp);
            SetComponents(ent, dataComp, "Generic", 2, 4);
        }
        else
        {
            if (dataComp.Document != null)
            {
                OnArchonDeath((ent.Owner, dataComp));
            }
            else
            {
                QueueDel(ent);
            }
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

        var content = paperComp.Content;
        if (!string.IsNullOrEmpty(content))
        {
            content = content.Replace("Статус объекта: Под наблюдением", "Статус объекта: [color=#cc0836]Списан[/color]");
            _paperSystem.SetContent(documentUid, content);
        }

        _paperSystem.TryStamp((documentUid, paperComp), new StampDisplayInfo
        {
            StampedName = "stamp-component-stamped-name-expunged",
            StampedColor = Color.FromHex("#8B0000")
        }, "paper_stamp-expunged");

        if (TryComp<ArchonComponent>(ent, out var comp))
        {
            Spawn(comp.RebirthPrototype, Transform(ent).Coordinates);
        }

        QueueDel(ent);
    }

    /// <summary>
    /// При попадании ХИД снарядом
    /// </summary>
    private void OnProjectileHit(EntityUid uid, StasisArchonOnCollideComponent comp, ref ProjectileHitEvent args)
    {
        if (!TryComp<ArchonDataComponent>(args.Target, out var dataComp))
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

        var archon = ent.Comp.Archon.Value;

        if (!TryComp<ArchonDataComponent>(archon, out var dataComp))
            return;

        dataComp.Expunged = true;
    }
}
