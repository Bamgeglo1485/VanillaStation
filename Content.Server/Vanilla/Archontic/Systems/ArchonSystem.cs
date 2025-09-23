using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Archontic.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Weapons.Melee;
using Content.Shared.Projectiles;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Pinpointer;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Atmos;

using Content.Shared.Inventory.Events;
using Content.Shared.Throwing;
using Content.Shared.Examine;
using Content.Shared.Hands;

using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

using Content.Server.Polymorph.Components;
using Content.Server.Spawners.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Station.Systems;
using Content.Server.Destructible;
using Content.Server.Pinpointer;
using Content.Server.Roles;

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
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        /// <summary>
        /// ArchonSystem
        /// </summary>
        SubscribeLocalEvent<StasisArchonOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ArchonDocumentComponent, ComponentShutdown>(OnDocumentDestroy);
        SubscribeLocalEvent<ArchonDataComponent, ComponentShutdown>(OnArchonDataShutdown);
        SubscribeLocalEvent<ArchonComponent, ComponentStartup>(OnComponentStartup);
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
        SubscribeLocalEvent<ArchonBriefingComponent, ComponentStartup>(GenerateBriefing);
        SubscribeLocalEvent<ArchonBriefingComponent, GetBriefingEvent>(OnGetBriefing);
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
        var polyQuery = EntityQueryEnumerator<PolymorphedEntityComponent>();
        var archonQuery = EntityQueryEnumerator<ArchonComponent, TransformComponent>();

        while (beaconQuery.MoveNext(out var uid, out var beaconComp, out var trans))
        {
            BeaconUpdate(uid, beaconComp, trans);
        }

        while (polyQuery.MoveNext(out var polyUid, out var polyComp))
        {
            if (!TryComp<ArchonComponent>(polyComp.Parent, out var comp))
                continue;

            ArchonStateUpdate(polyComp.Parent.Value, comp);
            Announce(polyUid, comp);
        }

        while (archonQuery.MoveNext(out var archon, out var comp, out var trans))
        {

            ArchonUpdate(archon, comp, trans);
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

    private void ArchonStateUpdate(EntityUid uid, ArchonComponent comp)
    {
        if ((_gameTiming.CurTime >= comp.StasisExit) && (comp.State == ArchonState.Stasis))
        {
            ForceStasisExit(uid, comp);
        }
    }

    private void ArchonUpdate(EntityUid uid, ArchonComponent comp, TransformComponent transComp)
    {
        // Телепорт архонта, если его выкинули в космос, ну или он сам, а ещё он перейдёт в awake с шансом 40 процентов
        if (comp.Comeback == true && Transform(uid).GridUid == null)
        {
            var map = Transform(uid).MapID;
            var validMinds = new ValueList<EntityUid>();
            var mindQuery = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, TransformComponent, MetaDataComponent>();

            while (mindQuery.MoveNext(out var targetUid, out var mc, out _, out var xform, out var meta))
            {
                if (mc.HasMind && !_container.IsEntityOrParentInContainer(targetUid, meta: meta, xform: xform) && xform.MapID == map)
                {
                    validMinds.Add(targetUid);
                }
            }

            if (validMinds.Count == 0)
                return;

            var target = _random.Pick(validMinds);

            _trans.SetCoordinates(uid, transComp, Transform(target).Coordinates);
            _popup.PopupEntity("Архонт материализуется рядом с вами", uid);

            if (comp.State != ArchonState.Awake && _random.Prob(comp.AwakeChance))
                ForceAwake(uid, comp);
            else
                _audio.PlayPvs(comp.ComebackSound, uid);
        }
    }

    private void Announce(EntityUid uid, ArchonComponent comp)
    {
        if (comp.Announcement == false || comp.AnnouncementPlayed)
            return;

        var trans = Transform(uid);

        EnsureComp<NavMapBeaconComponent>(uid);

        var stationUid = _station.GetStationInMap(trans.MapID);
        if (stationUid == null)
            return;

        var msg = Loc.GetString("archon-spawn-announcement",
        ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, trans)))));

        _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
        _audio.PlayGlobal("/Audio/Vanilla/Announcements/archonDetected.ogg", Filter.Broadcast(), true);
        _navMap.SetBeaconEnabled(uid, true);

        comp.AnnouncementPlayed = true;
    }

    /// <summary>
    /// Перемещение архонта в состояние стазиса
    /// </summary>
    public void ForceStasis(EntityUid uid, ArchonComponent comp)
    {

        comp.LastState = comp.State;
        comp.State = ArchonState.Stasis;

        EnsureComp<PolymorphableComponent>(uid);

        // Используем полиморфы, так как это наиболее лёгкий вариант, иначе нужно заморчиваться в заморозками всякими, в котором я не разбираюсь
        comp.PolymorphEntity = _morph.PolymorphEntity(uid, comp.StasisPrototype);
    }

    /// <summary>
    /// Перемещение архонта в обычное состояние
    /// </summary>
    public void ForceStasisExit(EntityUid uid, ArchonComponent comp)
    {

        if (!TryComp<PolymorphedEntityComponent>(comp.PolymorphEntity, out var polyComp) || comp.PolymorphEntity == null)
            return;

        comp.LastState = comp.State;
        comp.State = ArchonState.Basic;

        _morph.Revert(comp.PolymorphEntity.Value);
    }

    /// <summary>
    /// Перемещение архонта в состояние пробуждения
    /// </summary>
    public void ForceAwake(EntityUid uid, ArchonComponent comp)
    {
        if (comp.State == ArchonState.Awake)
            return;

        var dataComp = EnsureComp<ArchonDataComponent>(uid);

        comp.LastState = comp.State;
        comp.State = ArchonState.Awake;

        if (TryComp<ArchonHealthComponent>(uid, out var healthComp))
            healthComp.Health *= 3;

        if (TryComp<StaminaComponent>(uid, out var staminaComp))
            staminaComp.CritThreshold *= 3;

        //if (TryComp<ArchonGenerateComponent>(uid, out var genComp))
            //SetComponents(uid, dataComp, genComp, "Generic");

        SetArchonClass(dataComp);

        if (TryComp<DamageableComponent>(uid, out var damagComp))
           _damageableSystem.SetAllDamage(uid, damagComp, 0);

        Spawn(comp.AwakeEffect, Transform(uid).Coordinates);

        if (dataComp.Document is not { } documentUid)
            return;

        if (!TryComp<PaperComponent>(documentUid, out var paperComp) ||
            !TryComp<ArchonDocumentComponent>(documentUid, out var documentComp))
            return;

        if (documentComp.Archon == uid)
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

    private void SetDocumentClass(Entity<ArchonDataComponent> ent, string objclass, EntityUid documentUid, PaperComponent paperComp)
    {
        var content = paperComp.Content;
        var classMatch = ObjectClassRegex.Match(content);
        var objectClass = classMatch.Groups[1].Value;

        content = content.Replace(
        $"Класс объекта: {objectClass}",
        $"Класс объекта: {objclass}");

        _paperSystem.SetContent(documentUid, content);

    }

    /// <summary>
    /// Стазис архонта при спавне
    /// </summary>
    private void OnComponentStartup(EntityUid uid, ArchonComponent comp, ComponentStartup args)
    {
        comp.State = ArchonState.Stasis;
        comp.StasisExit = _gameTiming.CurTime + comp.StasisDelay;

        ForceStasis(uid, comp);
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

        if (dataComp.Destructibility == ArchonDestructibility.Rebirth && TryComp<ArchonComponent>(ent, out var archonComp))
        {
            SetDestructibility(ent, dataComp);
            ForceAwake(ent, archonComp);

            archonComp.State = ArchonState.Awake;
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
    private void OnArchonDataShutdown(Entity<ArchonDataComponent> ent, ref ComponentShutdown args)
    {
        OnArchonDeath(ent);
    }

    private void OnArchonDeath(Entity<ArchonDataComponent> ent)
    {

        Spawn(ent.Comp.DeathEffect, Transform(ent).Coordinates);

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

        RaiseLocalEvent(ent, new ArchonDeathEvent());
    }

    /// <summary>
    /// При попадании ХИД снарядом
    /// </summary>
    private void OnProjectileHit(EntityUid uid, StasisArchonOnCollideComponent comp, ref ProjectileHitEvent args)
    {
        if (!TryComp<ArchonComponent>(args.Target, out var archonComp))
            return;

        if (archonComp.State != ArchonState.Basic)
            return;

        if (archonComp.StasisHits >= archonComp.MaxHits)
            return;

        archonComp.StasisHits++;
        archonComp.StasisExit = _gameTiming.CurTime + comp.StasisDelay;

        ForceStasis(args.Target, archonComp);
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
