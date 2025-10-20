using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Vanilla.Archon.ShyGuy;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
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
        var user = args.User;

        if (!HasComp<MobStateComponent>(user))
            return;

        if (TryComp<BlindableComponent>(user, out var blind) && blind.IsBlind == true)
            return;

        if (!_mobStateSystem.IsAlive(user) || !_mobStateSystem.IsAlive(uid))
            return;

        if (!_examine.InRangeUnOccluded(user, uid, 16f))
            return;

        if (comp.Targets.Contains(user))
            return;

        RaiseNetworkEvent(new ShyGuyGazeEvent(GetNetEntity(uid), GetNetEntity(user)));

        if (comp.StingerSound != null)
            _audio.PlayLocal(comp.StingerSound, user, user);

        _popup.PopupClient("Беги", user, PopupType.LargeCaution);
    }
}
