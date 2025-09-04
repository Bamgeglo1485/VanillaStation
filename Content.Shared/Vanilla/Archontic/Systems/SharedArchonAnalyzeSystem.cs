using Content.Shared.Archontic.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Examine;

namespace Content.Shared.Archontic.Systems;

public sealed partial class SharedArchonAnalyzeSystem : EntitySystem
{

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArchonAnalyzerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ArchonAnalyzerComponent, ExaminedEvent>(OnAnalyzerExamine);

        SubscribeLocalEvent<ArchonScannerComponent, ExaminedEvent>(OnScannerExamine);
        SubscribeLocalEvent<ArchonScannerComponent, AfterInteractEvent>(OnInteract);

    }

    /// <summary>
    /// Система сканера архонтов
    /// </summary>

    private void OnInteract(EntityUid uid, ArchonScannerComponent comp, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!args.CanReach)
            return;

        if (TryComp<ArchonDataComponent>(target, out var dataComp))
        {

            comp.LinkedArchon = target;

            _audio.PlayPvs(comp.ScanSound, uid);
            _popup.PopupEntity($"Архон просканирован, сигнатура: {comp.LinkedArchon.Value}", uid);
        }
        else if (TryComp<ArchonAnalyzerComponent>(target, out var analyzerComp) && comp.LinkedArchon != null)
        {

            analyzerComp.LinkedArchon = comp.LinkedArchon;

            _audio.PlayPvs(comp.LoadSound, uid);
            _popup.PopupEntity("Сигнатура Архонта передана анализатору", uid);

        }
    }

    /// <summary>
    /// Дальше идут системы анализатора
    /// </summary>

    /// <summary>
    /// При вставлении бумажки
    /// </summary>
    private void OnItemSlotChanged(EntityUid uid, ArchonAnalyzerComponent comp, ContainerModifiedMessage args)
    {
        if (!comp.Initialized)
            return;

        if (args.Container.ID != comp.ItemSlot.ID)
            return;

        var paper = args.Entity;

    }

    private void OnAnalyzerExamine(EntityUid uid, ArchonAnalyzerComponent comp, ref ExaminedEvent args)
    {

        ShowArchonID(uid, comp.LinkedArchon, ref args);

    }

    private void OnScannerExamine(EntityUid uid, ArchonScannerComponent comp, ref ExaminedEvent args)
    {

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
