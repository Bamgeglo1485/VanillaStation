using Content.Shared.Archontic.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Vanilla.Overlays;
using Content.Shared.Overlays;
using Content.Client.Overlays;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory.Events;

namespace Content.Client.Vanilla.Overlays;

public sealed class ShowArchonClassSystem : EquipmentHudSystem<ShowArchonClassComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private bool showRealClass = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowArchonClassComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<ArchonDataComponent, GetStatusIconsEvent>(OnGetStatusIcons);

    }

    private void OnHandleState(Entity<ShowArchonClassComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        showRealClass = ent.Comp.ShowRealClass;

        RefreshOverlay();
    }

    private void OnGetStatusIcons(EntityUid uid, ArchonDataComponent dataComp, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        ProtoId<ArchonClassIconPrototype> iconId;

        if (dataComp.State == ArchonState.Awake)
            iconId = "Awake";

        if (showRealClass == true)
        {
            iconId = dataComp.Class.ToString();
        }
        else
        {
            if (dataComp.Document == null)
            {
                iconId = "Unknown";
            }
            else
            {
                iconId = dataComp.Class.ToString();
            }
        }

        if (!_prototype.TryIndex(iconId, out var iconPrototype))
            return;

        args.StatusIcons.Add(iconPrototype);
    }
}
