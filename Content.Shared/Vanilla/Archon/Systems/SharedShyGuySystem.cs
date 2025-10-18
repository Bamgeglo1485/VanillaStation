using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Audio;

using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Vanilla.Archon;

public sealed class SharedShyGuySystem : EntitySystem
{

    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShyGuyComponent, OutlineHoverEvent>(OnLook);
    }

    private void OnLook(EntityUid uid, ShyGuyComponent comp, OutlineHoverEvent args)
    {
        if (args.User == null)
            return;

        if (!HasComp<MobStateComponent>(args.User))
            return;

        if (comp.StingerSound != null)
            _audio.PlayLocal(comp.StingerSound, args.User.Value, args.User.Value);

        _popup.PopupClient("Беги", args.User.Value, args.User.Value, PopupType.LargeCaution);

        RaiseNetworkEvent(new ShyGuyGazeEvent(GetNetEntity(uid), GetNetEntity(args.User.Value)));
    }
}
