using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Archontic.Systems;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Throwing;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Paper;

using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem
{

    private void GenerateTests(EntityUid archonUid, ArchonDataComponent dataComp, EntityUid mtgUid, MTGComponent mtg)
    {
        if (dataComp.TestsGenerated)
        {
            _audio.PlayPvs(mtg.DenySound, mtgUid);
            _popup.PopupEntity("Тесты уже были сгенерированы для этого архонта", mtgUid);

            return;
        }

        if (dataComp.Document == null || dataComp.Expunged == true)
        {
            _audio.PlayPvs(mtg.DenySound, mtgUid);
            _popup.PopupEntity("Не обнаружено архонта в системе", mtgUid);

            return;
        }

        CreateTestPaper(mtgUid, mtg, archonUid, dataComp);

        dataComp.MTG = mtgUid;
        dataComp.TestsGenerated = true;
    }

    private EntityUid? CreateTestPaper(EntityUid mtgUid, MTGComponent mtg, EntityUid archonUid, ArchonDataComponent dataComp)
    {
        var paper = Spawn("Paper", Transform(mtgUid).Coordinates);

        var validTest = new List<ArchonTestPrototype>();

        // Подбор подходящих тестов
        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonTestPrototype>())
        {
            // Проверка на соответствие тэга
            if (proto.Tag != dataComp.GenericTag &&
                (dataComp.AdditiveTag != proto.Tag))
                continue;

            // Проверка на присутствие компонентов из вайтлиста
            if (proto.ComponentsWhitelist.Count > 0)
            {
                bool hasComponents = true;

                foreach (var componentType in proto.ComponentsWhitelist)
                {
                    var type = Type.GetType(componentType);
                    if (type == null || !HasComp(archonUid, type))
                    {
                        hasComponents = false;
                        break;
                    }
                }

                if (!hasComponents)
                    continue;
            }

            // Проверка на отсутствие компонентов из блеклиста
            if (proto.ComponentsBlacklist.Count > 0)
            {
                bool hasBlacklistedComponent = false;

                foreach (var componentType in proto.ComponentsBlacklist)
                {
                    var type = Type.GetType(componentType);
                    if (type != null && HasComp(archonUid, type))
                    {
                        hasBlacklistedComponent = true;
                        break;
                    }
                }

                if (hasBlacklistedComponent)
                    continue;
            }

            // Проверка на минимальный побег и опасность
            if (dataComp.Danger < proto.MinDangerLevel || dataComp.Escape < proto.MinEscapeLevel)
                continue;

            validTest.Add(proto);
        }

        var tests = validTest
            .OrderBy(x => _random.Next())
            .Take(mtg.MaxTests)
            .ToList();

        foreach (var testproto in tests)
        {
            dataComp.ActiveTests.Add(testproto.ID);
        }

        var content = new StringBuilder();
        content.AppendLine("=== ТЕСТЫ ДЛЯ АРХОНТА ===");
        content.AppendLine();

        foreach (var test in tests)
        {
            content.AppendLine($"Тест: {test.Desc}");
            content.AppendLine("---");
        }

        _paperSystem.SetContent(paper, content.ToString());

        _audio.PlayPvs(mtg.SuccessSound, mtgUid);
        _popup.PopupEntity("Тесты были сгенерированы", mtgUid);

        return paper;
    }

    private void CompleteTest(EntityUid uid, string testId, ArchonDataComponent comp)
    {
        comp.ActiveTests.Remove(testId);
        comp.CompletedTests.Add(testId);

        if (!TryComp<MTGComponent>(comp.MTG, out var mtgComp) || comp.MTG == null)
            return;

        var paper = Spawn("Paper", Transform(comp.MTG.Value).Coordinates);

        var content = new StringBuilder();
        content.AppendLine("=== ОТЧЁТ О ВЫПОЛНЕНИИ ТЕСТА ===");
        content.AppendLine();
        content.AppendLine($"Тест '{testId}' был успешно выполнен!");

        Spawn(mtgComp.AwardDisc, Transform(comp.MTG.Value).Coordinates);
        _paperSystem.SetContent(paper, content.ToString());

        if (comp.ActiveTests.Count > 0)
            return;

        var paperDesc = Spawn("Paper", Transform(comp.MTG.Value).Coordinates);
        var contentDesc = new StringBuilder();
        contentDesc.AppendLine("=== ДАННЫЕ С ТЕСТОВ ===");
        contentDesc.AppendLine();

        foreach (var proto in comp.AddedPrototypes)
        {
            contentDesc.AppendLine(proto.Desc);
        }

        _paperSystem.SetContent(paperDesc, contentDesc.ToString());

        _audio.PlayPvs(mtgComp.SuccessSound, comp.MTG.Value);
        _popup.PopupEntity("Тест успешно выполнен", comp.MTG.Value);
    }

    private void OnDeath(EntityUid uid, ArchonDataComponent comp, ref ArchonDeathEvent args)
    {
        if (comp.ActiveTests.Contains("Death"))
        {
            CompleteTest(uid, "Death", comp);
        }
    }

    private void OnBreach(EntityUid uid, ArchonDataComponent comp, ref ArchonBreachEvent args)
    {
        if (comp.ActiveTests.Contains("Breach"))
        {
            CompleteTest(uid, "Breach", comp);
        }
    }

    private void OnExamined(EntityUid uid, ArchonDataComponent comp, ref ExaminedEvent args)
    {
        if (comp.ActiveTests.Contains("Examine"))
        {
            CompleteTest(uid, "Examine", comp);
        }
    }

    private void OnActivate(EntityUid uid, ArchonDataComponent comp, ref ActivateInWorldEvent args)
    {
        if (comp.ActiveTests.Contains("Activate"))
        {
            CompleteTest(uid, "Activate", comp);
        }
    }

    private void OnDamageTest(EntityUid uid, ArchonDataComponent comp, ref DamageChangedEvent args)
    {
        if (comp.ActiveTests.Contains("Damage"))
        {
            CompleteTest(uid, "Damage", comp);
        }
    }

    private void OnHandSelected(EntityUid uid, ArchonDataComponent comp, ref HandSelectedEvent args)
    {
        if (comp.ActiveTests.Contains("Hand"))
        {
            CompleteTest(uid, "Hand", comp);
        }
    }

    private void OnThrow(EntityUid uid, ArchonDataComponent comp, ref ThrowEvent args)
    {
        if (comp.ActiveTests.Contains("Throw"))
        {
            CompleteTest(uid, "Throw", comp);
        }
    }

    private void OnThrowHit(EntityUid uid, ArchonDataComponent comp, ref ThrowDoHitEvent args)
    {
        if (comp.ActiveTests.Contains("ThrowHit"))
        {
            CompleteTest(uid, "ThrowHit", comp);
        }
    }

    private void OnEquip(EntityUid uid, ArchonDataComponent comp, ref GotEquippedEvent args)
    {
        if (comp.ActiveTests.Contains("Equip"))
        {
            CompleteTest(uid, "Equip", comp);
        }
    }
}
