using Content.Shared.Vanilla.Eye.Components;
using Content.Shared.Movement.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Vanilla.Eye;

public sealed class ClientBlindPredatorClientSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PredatorVisibleMarkComponent, ComponentStartup>(OnMarkStartup);
        SubscribeLocalEvent<PredatorVisibleMarkComponent, ComponentRemove>(OnMarkRemove);
    }

    private void OnMarkStartup(EntityUid uid, PredatorVisibleMarkComponent component, ComponentStartup args)
    {
        UpdateVisibility(uid);
    }

    private void OnMarkRemove(EntityUid uid, PredatorVisibleMarkComponent component, ComponentRemove args)
    {
        UpdateVisibility(uid);
    }

    private void UpdateVisibility(EntityUid uid)
    {
        if (HasComp<BlindPredatorComponent>(uid))
            return;

        if (!HasComp<InputMoverComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer?.ControlledEntity is not { } playerEntity)
        {
            _spriteSystem.SetVisible((uid, sprite), true);
            return;
        }

        if (!HasComp<BlindPredatorComponent>(playerEntity))
        {
            _spriteSystem.SetVisible((uid, sprite), true);
            return;
        }

        if (uid == playerEntity)
        {
            _spriteSystem.SetVisible((uid, sprite), true);
            return;
        }

        var isVisible = HasComp<PredatorVisibleMarkComponent>(uid);
        _spriteSystem.SetVisible((uid, sprite), isVisible);
    }

    private void UpdateAllMoversVisibility()
    {
        var query = EntityQueryEnumerator<InputMoverComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var mover, out var sprite))
        {
            UpdateVisibility(uid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateAllMoversVisibility();
    }
}
