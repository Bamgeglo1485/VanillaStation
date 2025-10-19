using Content.Shared.Popups;
using Content.Shared.Vanilla.Archon.ShyGuy;
using Content.Shared.Mobs.Components;

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
        if (!HasComp<MobStateComponent>(args.User))
            return;
        RaiseNetworkEvent(new ShyGuyGazeEvent(GetNetEntity(uid), GetNetEntity(args.User)));
        
        if (comp.State != ShyGuyState.Calm)
            return;

        if (comp.StingerSound != null)
            _audio.PlayLocal(comp.StingerSound, args.User, args.User);

        _popup.PopupClient("Беги", args.User, args.User, PopupType.LargeCaution);
    }

}
