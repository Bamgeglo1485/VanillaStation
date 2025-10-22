using Content.Shared.Vanilla.Archon.ShyGuy;
using Robust.Shared.Map;
using Robust.Shared.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.Player;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Client.Vanilla.Overlays;

public sealed class ShyGuyOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleShader = "CircleMask";
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SharedShyGuySystem _shyGuy;
    private readonly TransformSystem _transformSystem;
    private readonly ShaderInstance _circleMaskShader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true; // Обязательно

    private readonly float _range;
    public ShyGuyOverlay(float range)
    {
        IoCManager.InjectDependencies(this);

        _shyGuy = _entity.System<SharedShyGuySystem>();
        _transformSystem = _entity.System<TransformSystem>();
        _circleMaskShader = _prototypeManager.Index(CircleShader).InstanceUnique();
        _range = range;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity == null || ScreenTexture == null)
            return;

        var shyGuy = _playerManager.LocalEntity.Value;
        var eye = args.Viewport.Eye;
        var handle = args.WorldHandle;
        var isRaged = _shyGuy.IsRaged(shyGuy);
        if (isRaged)
        {
            var eyeRot = eye?.Rotation ?? default;
            var observers = _shyGuy.GetNearbyObservers(shyGuy, _range);
            foreach (var observer in observers)
            {
                if (!ValidateRender(observer, out var sprite, out var xform))
                    continue;

                Render((observer, sprite, xform), eye?.Position.MapId, handle, eyeRot);
            }
        }

        _circleMaskShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _circleMaskShader.SetParameter("Zoom", eye?.Zoom.X ?? 1.0f);
        _circleMaskShader.SetParameter("CircleMinDist", 0f);
        // Настройка шейдера
        _circleMaskShader.SetParameter("CircleRadius", isRaged ? 15f : 30f);
        _circleMaskShader.SetParameter("CircleMult", isRaged ? 0.5f : 1.0f);
        _circleMaskShader.SetParameter("CirclePow", isRaged ? 0.5f : 0.2f);
        _circleMaskShader.SetParameter("CircleMax", isRaged ? 0.5f : 0.25f);

        // Рисуем фильтр
        handle.UseShader(_circleMaskShader);
        handle.DrawRect(args.WorldBounds, isRaged ? Color.Red : Color.Gray);
        handle.UseShader(null);
    }

    private void Render(Entity<SpriteComponent, TransformComponent> ent, MapId? map, DrawingHandleWorld handle, Angle eyeRot)
    {
        var (_, sprite, xform) = ent;
        if (xform.MapID != map)
            return;

        var position = _transformSystem.GetWorldPosition(xform);
        var rotation = _transformSystem.GetWorldRotation(xform);

        var oldColor = sprite.Color;
        sprite.Color = Color.Red;
        sprite.Render(handle, eyeRot, rotation, position: position);
        sprite.Color = oldColor;
    }
    //получаем спрайт и координаты
    private bool ValidateRender(EntityUid target, [NotNullWhen(true)] out SpriteComponent? sprite, [NotNullWhen(true)] out TransformComponent? xform)
    {
        sprite = null;
        xform = null;

        _entity.TryGetComponent<SpriteComponent>(target, out sprite);
        _entity.TryGetComponent<TransformComponent>(target, out xform);

        return (sprite != null && xform != null);
    }
}