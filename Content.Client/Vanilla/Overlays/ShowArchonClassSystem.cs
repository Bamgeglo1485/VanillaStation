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
        SubscribeLocalEvent<ShowArchonClassComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ArchonDataComponent, GetStatusIconsEvent>(OnGetStatusIcons);

    }

    private void OnMapInit(Entity<ShowArchonClassComponent> ent, ref MapInitEvent args)
    {
        showRealClass = ent.Comp.ShowRealClass;
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

        if (TryComp<ArchonComponent>(uid, out var archonComp) && archonComp.State == ArchonState.Awake)
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
