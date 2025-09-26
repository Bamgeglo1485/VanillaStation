using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

using Content.Shared.Archontic.Components;
using Content.Shared.Examine;

namespace Content.Shared.Archontic.Systems;

public sealed partial class SharedArchonSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArchonAnalyzerComponent, ExaminedEvent>(OnAnalyzerExamine);
        SubscribeLocalEvent<ArchonScannerComponent, ExaminedEvent>(OnScannerExamine);
        SubscribeLocalEvent<ArchonBeaconComponent, ExaminedEvent>(OnBeaconExamine);

    }

    private void OnAnalyzerExamine(EntityUid uid, ArchonAnalyzerComponent comp, ref ExaminedEvent args)
    {

        ShowArchonID(uid, comp.LinkedArchon, ref args);

    }
    private void OnScannerExamine(EntityUid uid, ArchonScannerComponent comp, ref ExaminedEvent args)
    {

        ShowArchonID(uid, comp.LinkedArchon, ref args);

    }
    private void OnBeaconExamine(EntityUid uid, ArchonBeaconComponent comp, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        ShowArchonID(uid, comp.LinkedArchon, ref args);
    }

    private void ShowArchonID(EntityUid uid, EntityUid? linkedArchon, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (linkedArchon != null)
            args.PushMarkup($"Привязан архонт с сигнатурой: {linkedArchon.Value}");
    }
}
