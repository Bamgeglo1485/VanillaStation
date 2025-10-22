using Content.Shared.Popups;
using Content.Shared.Vanilla.Archon.ShyGuy;
using Content.Client.Vanilla.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Client.Player;

namespace Content.Client.Vanilla.Archon;

public sealed class ShyGuySystem : SharedShyGuySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private ShyGuyOverlay _overlay = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShyGuyComponent, OutlineHoverEvent>(OnLook);

        //оверлей
        SubscribeLocalEvent<ShyGuyComponent, ComponentInit>(OnShyInit);
        SubscribeLocalEvent<ShyGuyComponent, ComponentShutdown>(OnShyShutdown);
        SubscribeLocalEvent<ShyGuyComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShyGuyComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        _overlay = new(16f);
    }

    #region overlay
    private void OnPlayerAttached(EntityUid uid, ShyGuyComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }
    private void OnPlayerDetached(EntityUid uid, ShyGuyComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
    private void OnShyInit(EntityUid uid, ShyGuyComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
        {
            _overlayMan.AddOverlay(_overlay);
        }

    }
    private void OnShyShutdown(EntityUid uid, ShyGuyComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
            _overlayMan.RemoveOverlay(_overlay);
    }
    #endregion

    private void OnLook(EntityUid uid, ShyGuyComponent comp, OutlineHoverEvent args)
    {
        var user = args.User;

        if (!IsReachable(uid, user, comp))
            return;

        comp.Targets.Add(user);
        _audio.PlayLocal(comp.StingerSound, user, user);
        _popup.PopupClient("Беги", user, PopupType.LargeCaution);

        RaiseNetworkEvent(new ShyGuyGazeEvent(GetNetEntity(uid), GetNetEntity(user)));
    }
}
