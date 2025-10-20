using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Vanilla.Archon.ShyGuy;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;

namespace Content.Client.Vanilla.Archon;

public sealed class ShyGuySystem : SharedShyGuySystem
{

    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShyGuyComponent, OutlineHoverEvent>(OnLook);
    }

    private void OnLook(EntityUid uid, ShyGuyComponent comp, OutlineHoverEvent args)
    {
        if (!HasComp<MobStateComponent>(args.User))
            return;

        if (TryComp<BlindableComponent>(user, out var blind) && blind.IsBlind == true)
            return;

        if (!_mobStateSystem.IsAlive(user) || !_mobStateSystem.IsAlive(shyGuy))
            return;

        if (!_examine.InRangeUnOccluded(user, shyGuy, 16f))
            return;

        if (comp.Targets.Contains(args.User))
            return;

        RaiseNetworkEvent(new ShyGuyGazeEvent(GetNetEntity(uid), GetNetEntity(args.User)));

        if (comp.StingerSound != null)
            _audio.PlayLocal(comp.StingerSound, args.User, args.User);

        _popup.PopupClient("Беги", args.User, PopupType.LargeCaution);
    }
}
