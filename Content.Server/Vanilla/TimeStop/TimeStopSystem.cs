using Robust.Server.GameObjects;

using Content.Server.NPC.HTN;

using Content.Shared.Movement.Components;
using Content.Shared.Vanilla.TimeStop;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Events;
using Content.Shared.Projectiles;
using Content.Shared.Damage;
using Content.Shared.Ghost;
using Content.Shared.Item;

using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

using System.Numerics;

namespace Content.Server.Vanilla.TimeStop;

public sealed class TimeStopSystem : EntitySystem
{

    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimeStoppedComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<TimeStoppedComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<TimeStoppedComponent, ComponentShutdown>(OnTimeStoppedShutdown);

        SubscribeLocalEvent<TimeStopFieldComponent, ComponentShutdown>(OnFieldShutdown);

        SubscribeLocalEvent<TimeStopEvent>(OnTimeStopEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TimeStopFieldComponent, TransformComponent>();
        var curTime = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (curTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = curTime + comp.UpdateSpeed;

            var entities = _lookup.GetEntitiesInRange(xform.Coordinates, comp.Radius);

            if (entities == null || entities.Count == 0)
                continue;

            foreach (var ent in entities)
            {
                if (!Exists(ent) || Deleted(ent))
                    continue;

                if (ent == uid || comp.TimeStoppedEntities.Contains(ent) || ent == Transform(uid).ParentUid || comp.TimeStopIgnored.Contains(ent))
                    continue;

                if (HasComp<TimeStoppedComponent>(ent))
                    continue;

                if (HasComp<TimeStopImmunityComponent>(ent) && comp.IgnoreImmunity == false)
                    continue;

                if (HasComp<GhostComponent>(ent))
                    continue;

                if (!HasComp<ItemComponent>(ent) && !HasComp<ProjectileComponent>(ent) && !HasComp<InputMoverComponent>(ent) && !HasComp<HTNComponent>(ent))
                    continue;

                if (!TryComp<PhysicsComponent>(ent, out var physics))
                    continue;

                FreezeEntity(ent, physics, comp);
            }
        }
    }

    private void OnFieldShutdown(EntityUid uid, TimeStopFieldComponent comp, ComponentShutdown args)
    {
        comp.Radius = 0;

        foreach (var ent in comp.TimeStoppedEntities)
        {
            if (ent == null)
                continue;

            if (!TryComp<TimeStoppedComponent>(ent, out var timeStopped))
                continue;

            if (timeStopped.TimeStops > 1)
            {
                timeStopped.TimeStops--;
            }
            else
            {
                RemComp<TimeStoppedComponent>(ent);
            }
        }
    }

    private void OnTimeStoppedShutdown(EntityUid uid, TimeStoppedComponent comp, ComponentShutdown args)
    {
        UnfreezeEntity(uid, comp);
    }

    public void FreezeEntity(EntityUid entity, PhysicsComponent physics, TimeStopFieldComponent field)
    {
        if (!Exists(entity) || Deleted(entity))
            return;

        var timeStopped = AddComp<TimeStoppedComponent>(entity);
        timeStopped.BodyType = physics.BodyType;
        timeStopped.LinearVelocity = physics.LinearVelocity;
        timeStopped.AngularVelocity = physics.AngularVelocity;

        _physics.SetBodyType(entity, BodyType.Static, body: physics);
        _physics.SetLinearVelocity(entity, Vector2.Zero, body: physics);
        _physics.SetAngularVelocity(entity, 0f, body: physics);

        field.TimeStoppedEntities.Add(entity);

        _meta.SetEntityPaused(entity, true);
    }

    public void UnfreezeEntity(EntityUid entity, TimeStoppedComponent comp)
    {
        if (!Exists(entity) || Deleted(entity))
            return;

        comp.Enabled = false;

        _physics.SetBodyType(entity, comp.BodyType);
        _physics.SetLinearVelocity(entity, comp.LinearVelocity);
        _physics.SetAngularVelocity(entity, comp.AngularVelocity);

        _meta.SetEntityPaused(entity, false);

        _damageableSystem.ChangeDamage(entity, comp.StoredDamage);
        _stamina.TakeStaminaDamage(entity, comp.StoredStaminaDamage);
    }

    private void OnTimeStopEvent(TimeStopEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;

        args.Handled = true;

        var timeStop = Spawn(args.Prototype, Transform(uid).Coordinates);

        if (!TryComp<TimeStopFieldComponent>(timeStop, out var fieldComp))
        {
            QueueDel(timeStop);
            return;
        }

        fieldComp.TimeStopIgnored.Add(uid);
    }

    private void OnBeforeDamageChanged(EntityUid uid, TimeStoppedComponent comp, ref BeforeDamageChangedEvent args)
    {
        if (comp.Enabled == false)
            return;

        comp.StoredDamage += args.Damage;

        args.Cancelled = true;
    }

    private void OnBeforeStaminaDamage(EntityUid uid, TimeStoppedComponent comp, ref BeforeStaminaDamageEvent args)
    {
        if (comp.Enabled == false)
            return;

        comp.StoredStaminaDamage += args.Value;

        args.Cancelled = true;
    }
}
