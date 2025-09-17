using Content.Shared.Vanilla.Eye.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Fluids.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Vanilla.Eye;

public sealed class BlindPredatorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlindPredatorComponent, ComponentInit>(OnBlindCompInit);
        SubscribeLocalEvent<BlindPredatorComponent, ComponentRemove>(OnBlindCompRemove);
    }

    private void OnBlindCompInit(EntityUid uid, BlindPredatorComponent component, ComponentInit args)
    {
        UpdateAllEntities();
    }

    private void OnBlindCompRemove(EntityUid uid, BlindPredatorComponent component, ComponentRemove args)
    {
        var query = EntityQueryEnumerator<InputMoverComponent, SpriteComponent>();
        while (query.MoveNext(out var entity, out _, out var sprite))
        {
            _spriteSystem.SetVisible((entity, sprite), true);
        }
    }

    private void UpdateVisibility(EntityUid uid, SpriteComponent sprite, EntityUid predatorUid, BlindPredatorComponent blindComp)
    {
        if (!blindComp.Enabled)
        {
            if (_gameTiming.CurTime >= blindComp.EnableTime && blindComp.DelayEnabled)
            {
                blindComp.Enabled = true;
                blindComp.DelayEnabled = false;
            }
            else
            {
                _spriteSystem.SetVisible((uid, sprite), true);
                return;
            }
        }

        if (uid == predatorUid)
        {
            _spriteSystem.SetVisible((uid, sprite), true);
            return;
        }

        if (!HasComp<InputMoverComponent>(uid))
        {
            _spriteSystem.SetVisible((uid, sprite), true);
            return;
        }

        var mod = 1f;

        if (TryComp<InputMoverComponent>(predatorUid, out var mover) && mover.Sprinting)
        {
            mod = blindComp.UserRunModifier;
        }

        float visibleDistance = GetDistance(uid, blindComp) * mod;
        bool isVisible = InRange(predatorUid, uid, visibleDistance);
        _spriteSystem.SetVisible((uid, sprite), isVisible);
    }

    private float GetDistance(EntityUid targetUid, BlindPredatorComponent blindComp)
    {
        if (TryComp<PhysicsComponent>(targetUid, out var physics) && physics.LinearVelocity.Length() < 0.1f)
            return blindComp.VisibleDistanceStand;

        if (TryComp<InputMoverComponent>(targetUid, out var mover))
        {
            return mover.Sprinting ? blindComp.VisibleDistanceRun : blindComp.VisibleDistanceWalk;
        }

        return blindComp.VisibleDistanceWalk;
    }

    private bool InRange(EntityUid sourceUid, EntityUid targetUid, float maxDistance)
    {
        if (!TryComp<TransformComponent>(sourceUid, out var sourceTransform) ||
            !TryComp<TransformComponent>(targetUid, out var targetTransform) ||
            sourceTransform.MapID != targetTransform.MapID)
        {
            return false;
        }

        var sourcePos = _transform.GetWorldPosition(sourceTransform);
        var targetPos = _transform.GetWorldPosition(targetTransform);
        var distance = (sourcePos - targetPos).Length();

        return distance <= maxDistance;
    }

    private void UpdateAllEntities()
    {
        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer?.ControlledEntity is not { } predatorUid ||
            !TryComp<BlindPredatorComponent>(predatorUid, out var blindComp))
        {
            var query = EntityQueryEnumerator<InputMoverComponent, SpriteComponent>();
            while (query.MoveNext(out var entity, out _, out var sprite))
            {
                _spriteSystem.SetVisible((entity, sprite), true);
            }
            return;
        }

        var updateQuery = EntityQueryEnumerator<InputMoverComponent, SpriteComponent>();
        while (updateQuery.MoveNext(out var entity, out _, out var sprite))
        {
            UpdateVisibility(entity, sprite, predatorUid, blindComp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateAllEntities();
    }
}
