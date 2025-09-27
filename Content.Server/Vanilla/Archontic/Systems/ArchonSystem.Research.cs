using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Archontic.Systems;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Paper;
using Content.Shared.Radio;

using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

using Content.Server.Radio.EntitySystems;
using Content.Server.Research.Systems;

using System.Text.RegularExpressions;
using System.Text;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem : EntitySystem
{

    private static readonly Dictionary<string, ArchonClass> ArchonClasses = new()
    {
    {"Безопасный", ArchonClass.Safe},
    {"Евклид", ArchonClass.Euclid},
    {"Кетер", ArchonClass.Keter},
    {"Таумиэль", ArchonClass.Thaumiel}
    };

    private List<EntityUid> RegisteredArchons = new();
    private List<String> RegisteredNumbers = new();

    /// <summary>
    /// Очистка зарегистрированных номеров и айди архонтов
    /// </summary>
    private void OnRoundEnded(RoundStartedEvent args)
    {
        RegisteredArchons.Clear();
        RegisteredNumbers.Clear();
    }

    /// <summary>
    /// Система сканера архонтов
    /// </summary>
    private void OnInteract(EntityUid uid, ArchonScannerComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        if (!args.CanReach)
            return;

        // При сканировании архонта
        if (TryComp<ArchonDataComponent>(target, out var dataComp))
        {
            comp.LinkedArchon = target;

            _audio.PlayPvs(comp.ScanSound, uid);
            _popup.PopupEntity($"Архон просканирован, сигнатура: {comp.LinkedArchon.Value}", uid);
        }
        // Передача архонта анализатору
        else if (TryComp<ArchonAnalyzerComponent>(target, out var analyzerComp) && comp.LinkedArchon != null && _power.IsPowered(target))
        {
            analyzerComp.LinkedArchon = comp.LinkedArchon;

            _audio.PlayPvs(comp.LoadSound, uid);
            _popup.PopupEntity("Сигнатура Архонта передана анализатору", uid);
        }
        // Передача архонта маяку
        else if (TryComp<ArchonBeaconComponent>(target, out var beaconComp) && TryComp<ArchonDataComponent>(comp.LinkedArchon, out var dataComp2) && comp.LinkedArchon != null && _power.IsPowered(target))
        {
            if (dataComp2.Document == null)
                return;

            beaconComp.LinkedArchon = comp.LinkedArchon;

            if (dataComp2.Beacon != null && TryComp<ArchonBeaconComponent>(dataComp2.Beacon, out var beaconCompToNull))
            {
                beaconCompToNull.LinkedArchon = null;
            }

            dataComp2.Beacon = target;

            _audio.PlayPvs(comp.LoadSound, uid);
            _popup.PopupEntity("Сигнатура Архонта передана маяку", uid);

        }
    }

    /// <summary>
    /// Система сканера архонтов
    /// </summary>
    private void OnItemSlotChanged(EntityUid uid, ArchonAnalyzerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != comp.SlotId)
            return;

        var errors = new List<string> { };

        if (comp.LinkedArchon == null || !TryComp<ArchonDataComponent>(comp.LinkedArchon.Value, out var dataComp))
        {
            _container.RemoveEntity(uid, args.Entity);

            _audio.PlayPvs(comp.DenySound, uid);
            _popup.PopupEntity("Ошибка", uid);

            errors.Add("Анализатор не имеет привязанного к себе Архонта");

            return;
        }

        comp.AnalyzeEnd = _gameTiming.CurTime + comp.AnalyzeDelay;
        _ambientSound.SetAmbience(uid, true);
        comp.Analyzing = true;
        comp.Paper = args.Entity;
    }

    private void AnalyzerUpdate(EntityUid uid, ArchonAnalyzerComponent comp)
    {
        _ambientSound.SetAmbience(uid, false);
        comp.Analyzing = false;

        Analyze(uid, comp);
    }

    private void Analyze(EntityUid uid, ArchonAnalyzerComponent comp)
    {
        comp.Analyzing = false;

        if (!TryComp<ArchonDataComponent>(comp.LinkedArchon, out var dataComp))
            return;

    }

}
