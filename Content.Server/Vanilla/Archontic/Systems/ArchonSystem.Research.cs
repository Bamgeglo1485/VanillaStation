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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _trans = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    public static readonly Regex ObjectNumberRegex = new Regex(@"Объект №:\s*ACO-\d+-NT", RegexOptions.IgnoreCase);
    public static readonly Regex ObjectNameRegex = new Regex(@"Название объекта:\s*(.{1,20})", RegexOptions.IgnoreCase);
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
    /// Очистка зарегистрированных номеров и айди архонтов
    /// </summary>
    private void OnRoundEnded (RoundStartedEvent args)
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

            SetClass(target, beaconComp);

        }
        // Передача архонта ГЗТ
        else if (TryComp<MTGComponent>(target, out var mtgComp) && TryComp<ArchonDataComponent>(comp.LinkedArchon, out var dataComp3) && comp.LinkedArchon != null && _power.IsPowered(target))
        {
            GenerateTests(comp.LinkedArchon.Value, dataComp3, target, mtgComp);
        }
    }

    /// <summary>
    /// При вставлении бумажки в анализатор
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
            SetErrors(Print(uid, comp), errors);

            return;
        }

        if (!TryComp<PaperComponent>(args.Entity, out var paperComp))
        {
            _container.RemoveEntity(uid, args.Entity);

            _audio.PlayPvs(comp.DenySound, uid);
            _popup.PopupEntity("Ошибка", uid);

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
            else if (objectNumber == "Объект №: ACO-69-NT" || objectNumber == "Объект №: ACO-1488-NT")
            {
                errors.Add($"[color=color=#cc0836]Сообщение от Администратора:[/color] Воу, ввёл такие смешные цифры, и думаешь что тот ещё юморист? Скажу тебе одно - да пошёл ты ДАННЫЕ УДАЛЕНЫ");
            }
        }

        if (RegisteredArchons.Contains(comp.LinkedArchon.Value))
        {
            errors.Add("Данный архонт уже зарегистрирован в системе");
        }

        var nameMatch = ObjectNameRegex.Match(content);
        var objectName = nameMatch.Groups[1].Value.Trim();

        if (objectName.Length > 20)
        {
            errors.Add("Название объекта не должно превышать 20 символов");
        }

        if (errors.Count > 0)
        {
            _container.RemoveEntity(uid, args.Entity);

            _audio.PlayPvs(comp.DenySound, uid);
            _popup.PopupEntity("Обнаружены ошибки в документе", uid);

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

                // Ставит в документе правильный класс
                var adaptedClass = dataComp.Class switch
                {
                    ArchonClass.Safe => "[color=#0aad49]Безопасный[/color]",
                    ArchonClass.Euclid => "[color=#ba9307]Евклид[/color]",
                    ArchonClass.Keter => "[color=#cc4608]Кетер[/color]",
                    ArchonClass.Thaumiel => "[color=#cc0836]Таумиэль[/color]"
                };

                content = content.Replace(
                    $"Класс объекта: {objectClass}",
                    $"Класс объекта: {adaptedClass}");

                _paperSystem.SetContent(paper, content);
            }
        }

        // Устанавливает названия объекта с документа Архонту, почему бы нет
        _metaData.SetEntityName(comp.LinkedArchon.Value, objectName);

        RegisteredNumbers.Add(numberMatch.Value.Trim());
        RegisteredArchons.Add(comp.LinkedArchon.Value);

        _audio.PlayPvs(comp.SuccessSound, uid);
        _popup.PopupEntity("Документ принят и проверен", uid);

        // Дальше создание синхронированного документа
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

        _archonSystem.DirtyArchon(comp.LinkedArchon.Value, dataComp);

        SetSuccess(Print(uid, comp), errors);

    }

    /// <summary>
    /// Проверяет достоверность формата документа
    /// </summary>
    private List<string> CheckDocumentFormat(string content)
    {
        var errors = new List<string>();

        if (!ObjectNumberRegex.IsMatch(content))
            errors.Add("Неверный формат номера объекта. Требуется: 'Объект №: ACO-ЧИСЛО-NT'");

        if (!ObjectNameRegex.IsMatch(content))
        {
            errors.Add("Отсутствует наименование объекта.");
        }

        if (!ObjectStatusRegex.IsMatch(content))
            errors.Add("Отсутствует или неправилен статус объекта. Статус может быть только Под Наблюдением");

        if (!ObjectClassRegex.IsMatch(content))
            errors.Add("Ложный класс объекта. Допустимые: Безопасный, Евклид, Кетер, Таумиэль");

        if (!ContainmentRegex.IsMatch(content))
            errors.Add("Особые условия содержания должны содержать минимум 50 символов");

        if (!DescriptionRegex.IsMatch(content))
            errors.Add("Описание должно содержать минимум 50 символов");

        return errors;
    }

    /// <summary>
    /// Делает бумажку
    /// </summary>
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

    /// <summary>
    /// Делает лист с ошибками, большой щиткод
    /// </summary>
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
        text.AppendLine("[italic]Требуется повторная подача документа после исправления всех ошибок. Запомните, что попытки подделки документов или обмана автоматической системы могут и будут отслеживаться БЮРДЕПом фонда Н.З.П, если те будут обнаружены, мы отправим запрос о дисциплинарном взыскании вашему локальному Центральному Командованию[/italic]");

        text.AppendLine();
        text.AppendLine("РЕКОМЕНДАЦИИ:");
        text.AppendLine("─────────────");
        text.AppendLine("1. Напишите уникальный номер объекта в формате ACO-ЧИСЛО-NT");
        text.AppendLine("2. Напишите уникальное наименования объекта кратко описывающий его");
        text.AppendLine("3. Напишите класс объекта относительно его поведения. Безопасный - не имеющий опасности и возможности сбежать. Евклид - объект, стремящийся к побегу, но не приносящий значительный вред. Кетер - объект, носящий вред окружающей среде, но не имеющий возможности к побегу. Таумиэль - объект, который и сбегает, и приносит вред окружающему миру.");
        text.AppendLine("3. Напишите статус объекта, он обязательно должен быть: Под наблюдением");
        text.AppendLine("4. Опишите способ сдерживания объекта в камере.");
        text.AppendLine("5. Опишите внешность объекта и его свойства.");
        text.AppendLine("6. Напоминаем, что попытка подделки или обмана системы наказуемо вплоть до обжалования и штрафа до 10000 кредит, здраво описывайте объект и способ его содержания. Мы имеем удалённым доступ к зарегистрированным документам, и наш БЮРДЕП регулярно проверяет их содержания.");

        _paperSystem.SetContent(uid, text.ToString());
    }

    /// <summary>
    /// Делает лист с ошибками, ещё больший щиткод
    /// </summary>
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
        if (dataComp.Document == null || dataComp.Expunged == true ||
        !TryComp<TransformComponent>(comp.LinkedArchon, out var archonXform))
        {
            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.None);
            return false;
        }

        var beaconPos = _trans.GetWorldPosition(xform);
        var archonPos = _trans.GetWorldPosition(archonXform);
        var distance = (beaconPos - archonPos).Length();

        if (distance > comp.Radius && !dataComp.Expunged)
        {
            comp.Breached = true;
            _appearance.SetData(uid, ArchonBeaconVisuals.Classes, ArchonBeaconClasses.Breach);

            RaiseLocalEvent(comp.LinkedArchon.Value, new ArchonBreachEvent());

            string number = "Неизвестный";

            if (TryComp<PaperComponent>(dataComp.Document, out var paperComp))
            {
                var content = paperComp.Content;
                var numberMatch = ObjectNumberRegex.Match(content);

                if (numberMatch.Success)
                {
                    number = numberMatch.Value;
                }
            }

            var message = Loc.GetString("archon-breached-announcement", ("number", number));
            _radio.SendRadioMessage(uid, message, _prototypeManager.Index<RadioChannelPrototype>(comp.ScienceChannel), uid);

            return false;
        }

        if (comp.Breached)
        {
            comp.Breached = false;

            SetClass(uid, comp);
        }

        return true;
    }
}
