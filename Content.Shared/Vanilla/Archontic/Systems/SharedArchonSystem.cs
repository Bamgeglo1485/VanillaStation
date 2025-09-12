using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Graphics;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

using Content.Shared.Archontic.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Paper;

using System.Text.RegularExpressions;
using System.Text;

namespace Content.Shared.Archontic.Systems;

public sealed partial class SharedArchonSystem : EntitySystem
{
    
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArchonDataComponent, DirtyArchonEvent>(DirtyArchon);

        SubscribeLocalEvent<ArchonAnalyzerComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ArchonAnalyzerComponent, ExaminedEvent>(OnAnalyzerExamine);

        SubscribeLocalEvent<ArchonScannerComponent, ExaminedEvent>(OnScannerExamine);
        SubscribeLocalEvent<ArchonScannerComponent, AfterInteractEvent>(OnInteract);

        SubscribeLocalEvent<ArchonBeaconComponent, ExaminedEvent>(OnBeaconExamine);
    }

    // Хз как это работает, мне это дикпик выдал
    public static readonly Regex ObjectNumberRegex = new Regex(@"Объект №:\s*ACO-\d+-NT", RegexOptions.IgnoreCase);
    public static readonly Regex ObjectNameRegex = new Regex(@"Название объекта:\s*.+", RegexOptions.IgnoreCase);
    public static readonly Regex ObjectClassRegex = new Regex(@"Класс объекта:\s*(Безопасный|Евклид|Кетер|Таумиэль)", RegexOptions.IgnoreCase);
    public static readonly Regex ObjectStatusRegex = new Regex(@"Статус объекта:\s*(Под наблюдением)", RegexOptions.IgnoreCase);
    public static readonly Regex ContainmentRegex = new Regex(@"Особые условия содержания:\s*.{50,}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public static readonly Regex DescriptionRegex = new Regex(@"Описание:\s*.{50,}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
        else if (TryComp<ArchonAnalyzerComponent>(target, out var analyzerComp) && comp.LinkedArchon != null && _power.IsPowered(target))
        {

            analyzerComp.LinkedArchon = comp.LinkedArchon;

            _audio.PlayPvs(comp.LoadSound, uid);
            _popup.PopupEntity("Сигнатура Архонта передана анализатору", uid);

        }
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

            SetClass(target, beaconComp);

        }
    }

    /// <summary>
    /// Дальше идут системы анализатора
    /// </summary>

    /// <summary>
    /// При вставлении бумажки
    /// </summary>
    private void OnItemSlotChanged(EntityUid uid, ArchonAnalyzerComponent comp, EntInsertedIntoContainerMessage args)
    {

        var errors = new List<string> { };

        if (comp.LinkedArchon == null || !TryComp<ArchonDataComponent>(comp.LinkedArchon.Value, out var dataComp))
        {
            _container.RemoveEntity(uid, args.Entity);

            errors.Add("Анализатор не имеет привязанного к себе Архонта");
            SetErrors(Print(uid, comp), errors);

            return;
        }

        if (!TryComp<PaperComponent>(args.Entity, out var paperComp))
        {
            _container.RemoveEntity(uid, args.Entity);
            _audio.PlayPvs(comp.DenySound, uid);

            errors.Add("Ошибка при нанесении чернил");
            SetErrors(Print(uid, comp), errors);

            return;
        }

        var paper = args.Entity;
        var content = paperComp.Content;
        var numberMatch = ObjectNumberRegex.Match(content);

        errors = CheckDocumentFormat(content);

        if (numberMatch.Success)
        {
            var objectNumber = numberMatch.Value.Trim();
            if (RegisteredNumbers.Contains(objectNumber))
            {
                errors.Add($"Объект с номером {objectNumber} уже зарегистрирован в системе");
            }
        }

        if (RegisteredArchons.Contains(comp.LinkedArchon.Value))
        {
            errors.Add("Данный архонт уже зарегистрирован в системе");
        }

        if (errors.Count > 0)
        {
            _audio.PlayPvs(comp.DenySound, uid);
            _popup.PopupEntity("Обнаружены ошибки в документе", uid);
            _container.RemoveEntity(uid, args.Entity);

            SetErrors(Print(uid, comp), errors);

            return;
        }

        var classMatch = ObjectClassRegex.Match(content);
        if (classMatch.Success)
        {
            var objectClass = classMatch.Groups[1].Value;
            if (ArchonClasses.TryGetValue(objectClass, out var documentClass))
            {
                if (documentClass != dataComp.Class)
                {
                    errors.Add($"Класс объекта не соответствует предпологаемым. Предположительный ID класса: {dataComp.Class}");
                }
                else
                {
                    Spawn(comp.AwardPrototype, Transform(uid).Coordinates);
                }
            }
        }

        RegisteredNumbers.Add(numberMatch.Value.Trim());
        RegisteredArchons.Add(comp.LinkedArchon.Value);

        _audio.PlayPvs(comp.SuccessSound, uid);
        _popup.PopupEntity("Документ принят и проверен", uid);

        var document = Spawn(comp.DocumentPrototype, Transform(uid).Coordinates);

        if (TryComp<PaperComponent>(args.Entity, out var documentComp))
        {
            _paperSystem.SetContent(document, paperComp.Content);

            var archonDocumentComp = EnsureComp<ArchonDocumentComponent>(document);
            EnsureComp<PaperComponent>(document, out var documentPaperComp);
            archonDocumentComp.Archon = comp.LinkedArchon;
            dataComp.Document = document;

            _paperSystem.TryStamp((document, documentPaperComp), new StampDisplayInfo
            {
                StampedName = "stamp-component-stamped-name-archon",
                StampedColor = Color.FromHex("#000000")
            }, "paper_stamp-generic");
        }

        QueueDel(args.Entity);

        SetSuccess(Print(uid, comp), errors);
    }


    private List<string> CheckDocumentFormat(string content)
    {
        var errors = new List<string>();

        if (!ObjectNumberRegex.IsMatch(content))
            errors.Add("Неверный формат номера объекта. Требуется: 'Объект №: ACO-ЧИСЛО-NT'");

        if (!ObjectNameRegex.IsMatch(content))
            errors.Add("Отсутствует наименование объекта.");

        if (!ObjectStatusRegex.IsMatch(content))
            errors.Add("Статус может быть только Под Наблюдением");

        if (!ObjectClassRegex.IsMatch(content))
            errors.Add("Ложный класс объекта. Допустимые: Безопасный, Евклид, Кетер, Таумиэль");

        if (!ContainmentRegex.IsMatch(content))
            errors.Add("Особые условия содержания должны содержать минимум 50 символов");

        if (!DescriptionRegex.IsMatch(content))
            errors.Add("Описание должно содержать минимум 50 символов");

        return errors;
    }

    private EntityUid Print(EntityUid uid, ArchonAnalyzerComponent component)
    {
        
        var printed = Spawn(component.MachineOutput, Transform(uid).Coordinates);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            QueueDel(printed);
            return printed;
        }

        _metaData.SetEntityName(printed, ($"отчёт о анализе {component.LinkedArchon}"));

        var text = new StringBuilder();

        _paperSystem.SetContent((printed, paperComp), text.ToString());

        return printed;
    }

    // пипец щиткод
    private void SetErrors(EntityUid uid, List<string> errors)
    {
        var text = new StringBuilder();
        text.AppendLine("=== ОТЧЁТ О ПРОВЕРКЕ ДОКУМЕНТА ===");
        text.AppendLine();
        text.AppendLine("ОБНАРУЖЕННЫЕ ОШИБКИ:");
        text.AppendLine("───────────────────");

        for (int i = 0; i < errors.Count; i++)
        {
            text.AppendLine();
            text.Append(i + 1);
            text.Append(". ");
            text.Append(errors[i]);
        }
        text.AppendLine();

        text.AppendLine("ИТОГ:");
        text.AppendLine("─────────────");
        text.AppendLine("Статус: ДОКУМЕНТ НЕ ПРИНЯТ");
        text.AppendLine("[italic]Требуется повторная подача документа после исправления всех ошибок. Запомните, что попытки подделки документов или обмана автоматической системы могут и будут отслеживаться БЮРДЕПом фонда С.Р.С, если те будут обнаружены, мы отправим запрос о дисциплинарном взыскании вашему локальному Центральному Командованию[/italic]");

        text.AppendLine();
        text.AppendLine("РЕКОМЕНДАЦИИ:");
        text.AppendLine("─────────────");
        text.AppendLine("1. Напишите уникальный номер объекта в формате ACO-ЧИСЛО-NT");
        text.AppendLine("2. Напишите уникальное наименования объекта кратко описывающий его");
        text.AppendLine("3. Напишите класс объекта относительно его поведения. Безопасный - не имеющий опасности и возможности сбежать. Евклид - объект, стремящийся к побегу, но не приносящий значительный вред. Кетер - объект, носящий вред окружающей среде, но не имеющий возможности к побегу. Таумиэль - объект, который и сбегает, и приносит вред окружающему миру.");
        text.AppendLine("4. Опишите способ сдерживания объекта в камере.");
        text.AppendLine("5. Опишите внешность объекта и его свойства.");
        text.AppendLine("6. Напоминаем, что попытка подделки или обмана системы наказуемо вплоть до обжалования и штрафа до 10000 кредит, здраво описывайте объект и способ его содержания. Мы имеем удалённым доступ к зарегистрированным документам, и наш БЮРДЕП регулярно проверяет их содержания.");

        _paperSystem.SetContent(uid, text.ToString());
    }

    // ещё больший щиткод
    private void SetSuccess(EntityUid uid, List<string> errors)
    {
        var text = new StringBuilder();
        text.AppendLine("=== ОТЧЁТ О ПРОВЕРКЕ ДОКУМЕНТА ===");
        text.AppendLine();
        text.AppendLine("ОБНАРУЖЕННЫЕ ОШИБКИ:");
        text.AppendLine("───────────────────");

        for (int i = 0; i < errors.Count; i++)
        {
            text.AppendLine();
            text.Append(i + 1);
            text.Append(". ");
            text.Append(errors[i]);
        }
        text.AppendLine();

        text.AppendLine("ИТОГ:");
        text.AppendLine("─────────────");
        text.AppendLine("Статус: ДОКУМЕНТ ПРИНЯТ");

        _paperSystem.SetContent(uid, text.ToString());
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

    public void ClearData()
    {
        RegisteredArchons.Clear();
        RegisteredNumbers.Clear();
    }

    /// <summary>
    /// Дальше идут системы маяка
    /// </summary>

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ArchonBeaconComponent, TransformComponent>();
        var curTime = _gameTiming.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (curTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = curTime + TimeSpan.FromSeconds(comp.UpdateSpeed);

            if (!_power.IsPowered(uid))
            {
                _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.NonPowered);
                return;
            }

            if (!TryComp<ArchonDataComponent>(comp.LinkedArchon, out var dataComp))
            {
                _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.None);
                return;
            }

            if (CheckInContainment(uid, comp, dataComp, xform))
            {
                int mod = 1;

                if (comp.ModificatePointsByClass)
                {
                    mod = dataComp.Class switch
                    {
                        ArchonClass.Safe => 1,
                        ArchonClass.Euclid => 2,
                        ArchonClass.Keter => 3,
                        ArchonClass.Thaumiel => 4,
                    };
                }

                var points = comp.ResearchPointsPerSecond;
                RaiseLocalEvent(uid, new BeaconPointAddEvent(points * comp.UpdateSpeed));
            }
        }
    }

    /// <summary>
    /// Устанавливает визуал класса
    /// </summary>
    private void SetClass(EntityUid uid, ArchonBeaconComponent comp)
    {
        if (!TryComp<ArchonDataComponent>(comp.LinkedArchon, out var dataComp))
            return;

        var visualState = dataComp.Class switch
        {
            ArchonClass.Safe => ArchonBeaconClasses.Safe,
            ArchonClass.Euclid => ArchonBeaconClasses.Euclid,
            ArchonClass.Keter => ArchonBeaconClasses.Keter,
            ArchonClass.Thaumiel => ArchonBeaconClasses.Thaumiel
        };

        _appearance.SetData(uid, ArchonBeaconVisuals.Classes, visualState);
    }

    /// <summary>
    /// Проверка состояний архонта - сбежал, на содержании, не найден/списан
    /// </summary>
    private bool CheckInContainment(EntityUid uid, ArchonBeaconComponent comp, ArchonDataComponent dataComp, TransformComponent xform)
    {

        if (dataComp.Document == null || dataComp.Expunged == true)
        {
            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.None);
            return false;
        }

        if (!TryComp<TransformComponent>(comp.LinkedArchon, out var archonXform))
        {
            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.None);
            return false;
        }

        var distance = (archonXform.WorldPosition - xform.WorldPosition).Length();

        if (distance > comp.Radius && !dataComp.Expunged)
        {
            comp.Breached = true;

            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.Breach);
            return false;
        }

        if (comp.Breached == true)
        {
            comp.Breached = false;

            SetClass(uid, comp);
        }

        return true;
    }

    /// <summary>
    /// Дальше идут системы архонт генерации
    /// </summary>
    ///

    private void DirtyArchon(EntityUid uid, ArchonDataComponent comp, DirtyArchonEvent args)
    {
        Dirty(uid, comp);
    }
}
