using Content.Shared.Popups;
using Content.Shared.Vanilla.Archon.ShyGuy;

namespace Content.Client.Vanilla.Archon;

public sealed class ShyGuySystem : SharedShyGuySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShyGuyComponent, OutlineHoverEvent>(OnLook);
    }

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
    //туду оверлей
}
