using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Vanilla.Warmer;
using Content.Shared.Weapons.Melee;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Pinpointer;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Atmos;
using Content.Shared.Mobs;

using Content.Shared.Inventory.Events;
using Content.Shared.Throwing;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Audio;
using Content.Shared.Hands;

using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.IoC;

using Content.Server.Polymorph.Components;
using Content.Server.Spawners.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.Pinpointer;
using Content.Server.Roles;

using System.Linq;
using System.Text;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedTransformSystem _trans = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PolymorphSystem _morph = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        /// ArchonSystem
        SubscribeLocalEvent<StasisArchonOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ArchonComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<ArchonComponent, ComponentStartup>(OnComponentStartup);

        /// ArchonSystem.Generate
        SubscribeLocalEvent<ArchonRoleComponent, ComponentStartup>(GenerateBriefing);
        SubscribeLocalEvent<ArchonRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<ArchonGenerateComponent, MapInitEvent>(OnMapInit);

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

        var archonQuery = EntityQueryEnumerator<ArchonComponent, TransformComponent>();
        var polyQuery = EntityQueryEnumerator<PolymorphedEntityComponent>();

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

    private void ArchonStateUpdate(EntityUid uid, ArchonComponent comp)
    {
        if ((_gameTiming.CurTime >= comp.StasisExit) && (comp.State == ArchonState.Stasis))
        {
            ForceStasisExit(uid, comp);
        }
    }

    private void ArchonUpdate(EntityUid uid, ArchonComponent comp, TransformComponent transComp)
    {
        // Телепорт архонта, если его выкинули в космос, ну или он сам, а ещё он перейдёт в awake с каким то шансом
        if (comp.Comeback == true && Transform(uid).GridUid == null)
        {
            Comeback(uid, comp, transComp);
        }

        // Увеличение уровня синхронизации до базового
        if (_gameTiming.CurTime >= comp.NextSyncLevelRecover && comp.SyncLevel < comp.BaseSyncLevel && comp.State != ArchonState.Stasis)
        {
            comp.NextSyncLevelRecover = _gameTiming.CurTime + comp.SyncLevelRecoverDelay;

            ChangeSyncLevel(uid, comp, 1, comp.BaseSyncLevel);
        }
    }

    /// <summary>
    /// Возвращает архонта к случайному игроку
    /// </summary>
    public void Comeback(EntityUid uid, ArchonComponent comp, TransformComponent transComp)
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

        if (comp.State != ArchonState.Awake && _random.Prob(comp.AwakeChance * (comp.SyncLevel / (comp.MaxSyncLevel / 2))))
            ForceAwake(uid, comp);
        else
            _audio.PlayPvs(comp.ComebackSound, uid);
    }

    /// <summary>
    /// Перемещение архонта в состояние стазиса
    /// </summary>
    public void ForceStasis(EntityUid uid, ArchonComponent comp)
    {
        if (comp.StasisExit >= _gameTiming.CurTime)
        {
            comp.StasisExit = _gameTiming.CurTime + comp.StasisDelay;
        }

        comp.LastState = comp.State;
        comp.State = ArchonState.Stasis;

        ChangeSyncLevel(uid, comp, -10, 0, false);

        EnsureComp<PolymorphableComponent>(uid);

        // Используем полиморфы, так как это наиболее лёгкий вариант, иначе нужно заморчиваться с заморозками всякими, в которых я не разбираюсь
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
        comp.SyncLevel++;

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
        comp.BaseSyncLevel = 10;

        ChangeSyncLevel(uid, comp, 10, 10, false);

        if (TryComp<MobThresholdsComponent>(uid, out var thresComp))
            _mobThresSystem.SetMobStateThreshold(uid,
                _mobThresSystem.GetThresholdForState(uid, MobState.Dead, thresComp) * 3,
                MobState.Dead, thresComp);

        if (TryComp<StaminaComponent>(uid, out var staminaComp))
            staminaComp.CritThreshold *= 3;

        if (TryComp<DamageableComponent>(uid, out var damagComp))
           _damageableSystem.SetAllDamage(uid, damagComp, 0);

        Spawn(comp.AwakeEffect, Transform(uid).Coordinates);
    }

    /// <summary>
    /// Изменение кол-во степени синхронизации
    /// </summary>
    private void ChangeSyncLevel(EntityUid uid, ArchonComponent comp, int level, int maxLevel = 10, bool checkStasisAwake = true)
    {
        if (comp.State == ArchonState.Awake)
            return;

        comp.SyncLevel = Math.Clamp(comp.SyncLevel + level, 0, maxLevel);

        RevealFeatures(uid, comp);

        if (!checkStasisAwake)
            return;

        if (comp.SyncLevel >= 10)
            ForceAwake(uid, comp);
        else if (comp.SyncLevel <= 0)
            ForceStasis(uid, comp);

    }

    /// <summary>
    /// Раскрывает скрытые свойства
    /// </summary>
    private void RevealFeatures(EntityUid uid, ArchonComponent comp)
    {
        if (!TryComp<ArchonDataComponent>(uid, out var dataComp))
            return;

        var syncLevel = comp.SyncLevel;
        bool revealedAny = false;

        if (comp.SecretFeatures == null || comp.SecretFeatures.Count == 0)
            return;

        foreach (var secretFeature in comp.SecretFeatures)
        {
            if (secretFeature.Components == null)
                continue;

            if (syncLevel >= secretFeature.RevealThreshold && !secretFeature.Revealed)
            {
                EntityManager.AddComponents(uid, secretFeature.Components);

                dataComp.Danger += secretFeature.Danger;
                dataComp.Escape += secretFeature.Escape;

                secretFeature.Revealed = true;
                revealedAny = true;

                _popup.PopupEntity("Материя архонта на мгновение переливается", uid);
            }
        }

        if (revealedAny)
        {
            SetArchonClass(dataComp);
            Dirty(uid, dataComp);
            Dirty(uid, comp);
        }
    }

    /// <summary>
    /// Стазис архонта при спавне
    /// </summary>
    private void OnComponentStartup(EntityUid uid, ArchonComponent comp, ComponentStartup args)
    {
        if (comp.State != ArchonState.Stasis)
            return;

        comp.StasisExit = _gameTiming.CurTime + comp.SpawnStasisDelay;
        comp.NextSyncLevelRecover = _gameTiming.CurTime + comp.SyncLevelRecoverDelay;

        ForceStasis(uid, comp);
    }

    /// <summary>
    /// При смерти архонта
    /// </summary>
    private void OnMobStateChange(Entity<ArchonComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobStateSystem.IsAlive(ent))
            return;

        var comp = ent.Comp;

        if (comp.Rebirth)
        {
            comp.Rebirth = false;
            ForceAwake(ent, comp);
        }
        else 
        {
            OnArchonDeath(ent);
            QueueDel(ent);
        }
    }

    /// <summary>
    /// Инициализация смерти архонта
    /// </summary>
    private void OnArchonDeath(Entity<ArchonComponent> ent)
    {

        Spawn(ent.Comp.AwakeEffect, Transform(ent).Coordinates);
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

        if (archonComp.StasisHits >= comp.MaxHits)
            return;

        archonComp.StasisHits++;
        archonComp.StasisExit = _gameTiming.CurTime + comp.StasisDelay;
        archonComp.SyncLevel -= comp.SyncLevelDamage;

        ForceStasis(args.Target, archonComp);
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
}
