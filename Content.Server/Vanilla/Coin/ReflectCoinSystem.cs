using Content.Shared.Vanilla.Coin;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Mobs;

using Robust.Shared.Physics.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.Map;

using Content.Server.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Content.Shared.Damage;
using System.Numerics;


namespace Content.Server.Vanilla.Coin;

public sealed class ReflectCoinSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReflectCoinComponent, DamageChangedEvent>(OnDamageReceived);
        SubscribeLocalEvent<ReflectCoinComponent, AmmoShotEvent>(ModifyDamage);
        SubscribeLocalEvent<ReflectCoinComponent, ComponentStartup>(OnCoinStartup);
    }

    private void OnCoinStartup(EntityUid uid, ReflectCoinComponent component, ComponentStartup args)
    {
        var curTime = _gameTiming.CurTime;
        component.FlashingStartTime = curTime + TimeSpan.FromSeconds(1.5);
        component.FlashingEndTime = component.FlashingStartTime + TimeSpan.FromSeconds(0.5);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ReflectCoinComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.FlashingStartTime == null)
                continue;

            if (!component.Flashing && currentTime >= component.FlashingStartTime)
            {
                component.Flashing = true;
                if (component.FlashEffectPrototype != null)
                {
                    var coords = Transform(uid).Coordinates;
                    Spawn(component.FlashEffectPrototype, coords);
                }

                _audioSystem.PlayPvs(component.FlashSound, uid);
            }
            else if (component.Flashing && currentTime >= component.FlashingEndTime)
            {
                component.Flashing = false;
                component.FlashingStartTime = null;
                component.FlashingEndTime = null;
            }
        }
    }

    private void OnDamageReceived(EntityUid uid, ReflectCoinComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta?.GetTotal() <= 0f)
            return;

        if (!TryComp<GunComponent>(uid, out var gun) ||
            !HasComp<ProjectileBatteryAmmoProviderComponent>(uid))
            return;

        component.StoredDamage = args.DamageDelta;
        var target = FindCoinTarget(uid) ?? FindNpcTarget(uid);

        if (target == null)
            return;

        _gunSystem.AttemptShoot(uid, uid, gun, new EntityCoordinates(target.Value, Vector2.Zero));
    }

    private void ModifyDamage(EntityUid uid, ReflectCoinComponent component, AmmoShotEvent args)
    {
        if (component.StoredDamage == null)
            return;

        var modifier = component.Flashing ? component.FlashingDamageModifier : component.DamageModifier;
        var newDamage = new DamageSpecifier();

        foreach (var (damageType, damageValue) in component.StoredDamage.DamageDict)
        {
            newDamage.DamageDict[damageType] = damageValue * modifier;
        }

        foreach (var projectile in args.FiredProjectiles)
        {
            if (TryComp<ProjectileComponent>(projectile, out var projectileComp))
            {
                projectileComp.Damage = newDamage;
            }
        }

        QueueDel(uid);
    }

    private EntityUid? FindCoinTarget(EntityUid sourceUid)
    {
        var sourcePos = _transformSystem.GetWorldPosition(sourceUid);
        var nearestDistance = float.MaxValue;
        EntityUid? nearestTarget = null;

        var query = EntityQueryEnumerator<ReflectCoinComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == sourceUid)
                continue;

            var distance = (_transformSystem.GetWorldPosition(uid) - sourcePos).LengthSquared();

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = uid;
            }
        }

        return nearestTarget;
    }

    private EntityUid? FindNpcTarget(EntityUid sourceUid)
    {
        var sourcePos = _transformSystem.GetWorldPosition(sourceUid);
        var nearestDistance = float.MaxValue;
        EntityUid? nearestTarget = null;

        var query = EntityQueryEnumerator<NpcFactionMemberComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var factions, out var mobState))
        {
            if (uid == sourceUid || mobState.CurrentState != MobState.Alive)
            {
                continue;
            }

            var distance = (_transformSystem.GetWorldPosition(uid) - sourcePos).LengthSquared();

            if (distance < nearestDistance && AreEntitiesHostile(sourceUid, uid, factions))
            {
                nearestDistance = distance;
                nearestTarget = uid;
            }
        }

        return nearestTarget;
    }

    private bool AreEntitiesHostile(EntityUid sourceUid, EntityUid targetUid, NpcFactionMemberComponent? targetFactions = null)
    {
        if (!TryComp<NpcFactionMemberComponent>(sourceUid, out var sourceFactions) ||
            !TryComp(targetUid, out targetFactions))
            return true;

        foreach (var sourceFaction in sourceFactions.Factions)
        {
            foreach (var targetFaction in targetFactions.Factions)
            {
                if (_factionSystem.IsFactionHostile(sourceFaction, targetFaction))
                    return true;
            }
        }

        return false;
    }
}
