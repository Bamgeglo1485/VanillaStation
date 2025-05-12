using Content.Shared.Vanilla.Coin;
using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Projectiles;
using Robust.Shared.Player;
using Content.Shared.Weapons.Ranged.Events;
using System.Linq;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;
using System.Collections.Generic;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Vanilla.Coin;

public sealed class ReflectCoinSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private readonly HashSet<EntityUid> _excludedTargets = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReflectCoinComponent, DamageChangedEvent>(OnDamageReceived);
        SubscribeLocalEvent<ReflectCoinComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<ReflectCoinComponent, ComponentStartup>(OnCoinStartup);
    }

    private void OnCoinStartup(EntityUid uid, ReflectCoinComponent component, ComponentStartup args)
    {
        component.FlashingStartTime = _gameTiming.CurTime + TimeSpan.FromSeconds(1.5);
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
                    var transform = Transform(uid);
                    _entityManager.SpawnEntity(component.FlashEffectPrototype, transform.Coordinates);
                }

                 _audioSystem.PlayPvs(component.FlashSound, uid);
            }

            if (component.Flashing && currentTime >= component.FlashingEndTime)
            {
                component.Flashing = false;
                component.FlashingStartTime = null;
                component.FlashingEndTime = null;
            }
        }
    }

    private void OnDamageReceived(EntityUid uid, ReflectCoinComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= 0f)
            return;

        if (!TryComp<GunComponent>(uid, out var gun) ||
            !TryComp<ProjectileBatteryAmmoProviderComponent>(uid, out var ammo))
            return;

        component.StoredDamage = args.DamageDelta;

        var target = FindCoinTarget(uid) ?? FindNpcTarget(uid);

        if (target == null)
            return;

        var targetCoordinates = new EntityCoordinates(
            target.Value,
            Vector2.Zero
        );

        _gunSystem.AttemptShoot(
            uid,
            uid,
            gun,
            targetCoordinates
        );
        _audioSystem.PlayPvs(component.ReflectSound, uid);
    }

    private void OnAmmoShot(EntityUid uid, ReflectCoinComponent component, AmmoShotEvent args)
    {
        if (component.StoredDamage == null)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp<ProjectileComponent>(projectile, out var projectileComp))
                continue;

            var newDamage = new DamageSpecifier();

            foreach (var (damageType, damageValue) in component.StoredDamage.DamageDict)
            {
                if (component.Flashing)
                {
                    newDamage.DamageDict.Add(damageType, damageValue * component.FlashingDamageModifier);
                }
                else
                {
                    newDamage.DamageDict.Add(damageType, damageValue * component.DamageModifier);
                }
            }

            projectileComp.Damage = newDamage;
        }

        _entityManager.QueueDeleteEntity(uid);
    }

    private EntityUid? FindCoinTarget(EntityUid sourceUid)
    {
        var sourcePos = _transformSystem.GetWorldPosition(sourceUid);
        var nearestDistance = float.MaxValue;
        EntityUid? nearestTarget = null;

        var query = EntityQueryEnumerator<ReflectCoinComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == sourceUid || _excludedTargets.Contains(uid))
                continue;

            var pos = _transformSystem.GetWorldPosition(uid);
            var distance = (pos - sourcePos).LengthSquared();

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

        var query = EntityQueryEnumerator<NpcFactionMemberComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == sourceUid || _excludedTargets.Contains(uid))
                continue;

            var pos = _transformSystem.GetWorldPosition(uid);
            var distance = (pos - sourcePos).LengthSquared();

            if (distance < nearestDistance && AreEntitiesHostile(sourceUid, uid))
            {
                nearestDistance = distance;
                nearestTarget = uid;
            }
        }

        return nearestTarget;
    }

    private bool AreEntitiesHostile(EntityUid sourceUid, EntityUid targetUid)
    {
        if (!TryComp<NpcFactionMemberComponent>(sourceUid, out var sourceFactions) ||
            !TryComp<NpcFactionMemberComponent>(targetUid, out var targetFactions))
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
