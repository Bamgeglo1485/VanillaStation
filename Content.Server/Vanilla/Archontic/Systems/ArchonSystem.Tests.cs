using Content.Shared.Archontic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Archontic.Systems;
using Content.Shared.Interaction;
using Content.Shared.GameTicking;
using Content.Shared.Throwing;
using Content.Shared.Examine;
using Content.Shared.Dataset;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Paper;
using Content.Shared.Hands;

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

using Content.Server.Chat.Systems;

namespace Content.Server.Archontic.Systems;

public sealed partial class ArchonSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    private void GenerateTests(EntityUid archonUid, ArchonDataComponent dataComp, EntityUid mtgUid, MTGComponent mtgComp)
    {
        if (dataComp.TestsGenerated)
        {
            _audio.PlayPvs(mtgComp.DenySound, mtgUid);
            _chat.TrySendInGameICMessage(mtgUid, Loc.GetString("mtg-archon-duplicat"), InGameICChatType.Speak, true);

            var paper = Spawn(mtgComp.MachineOutput, Transform(mtgUid).Coordinates);

            var content = new StringBuilder();
            content.AppendLine("=== ТЕСТЫ ДЛЯ АРХОНТА ===");
            content.AppendLine();

            foreach (var test in dataComp.ActiveTests)
            {
                content.AppendLine($"Тест: {test}");
                content.AppendLine("---");
            }

            _paperSystem.SetContent(paper, content.ToString());
            return;
        }

        if (dataComp.Document == null || dataComp.Expunged)
        {
            _audio.PlayPvs(mtgComp.DenySound, mtgUid);
            _chat.TrySendInGameICMessage(mtgUid, Loc.GetString("mtg-archon-nonregistered"), InGameICChatType.Speak, true);
            return;
        }

        if (CreateTestPaper(mtgUid, mtgComp, archonUid, dataComp) != null)
        {
            dataComp.MTG = mtgUid;
            dataComp.TestsGenerated = true;
        }
    }

    private EntityUid? CreateTestPaper(EntityUid mtgUid, MTGComponent mtgComp, EntityUid archonUid, ArchonDataComponent dataComp)
    {
        var paper = Spawn(mtgComp.MachineOutput, Transform(mtgUid).Coordinates);
        var validTests = new List<ArchonTestPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<ArchonTestPrototype>())
        {
            // Проверка тегов
            if (proto.Tag != dataComp.GenericTag && dataComp.AdditiveTag != proto.Tag)
                continue;

            // Проверка вайтлиста компонентов
            if (!CheckComponentsWhitelist(archonUid, proto))
                continue;

            // Проверка уровней опасности и побега
            if (dataComp.Danger < proto.MinDangerLevel || dataComp.Escape < proto.MinEscapeLevel)
                continue;

            validTests.Add(proto);
        }

        if (validTests.Count == 0)
            return null;

        var selectedTests = validTests
            .OrderBy(x => _random.Next())
            .Take(mtgComp.MaxTests)
            .ToList();

        var content = new StringBuilder();
        content.AppendLine("=== ТЕСТЫ ДЛЯ АРХОНТА ===");
        content.AppendLine();

        foreach (var test in selectedTests)
        {
            dataComp.ActiveTests.Add(test.ID);
            content.AppendLine($"Тест: {test.Desc}");
            content.AppendLine("---");
        }
        content.AppendLine();

        if (TryGetRandomComment(mtgComp, out var comment))
            content.AppendLine(comment);

        _paperSystem.SetContent(paper, content.ToString());
        _audio.PlayPvs(mtgComp.SuccessSound, mtgUid);
        _chat.TrySendInGameICMessage(mtgUid, Loc.GetString("mtg-test-generated"), InGameICChatType.Speak, true);

        return paper;
    }

    private bool CheckComponentsWhitelist(EntityUid uid, ArchonTestPrototype proto)
    {
        if (proto.ComponentsWhitelist.Count > 0)
        {
            foreach (var componentType in proto.ComponentsWhitelist)
            {
                var type = Type.GetType(componentType);
                if (type == null || !HasComp(uid, type))
                    return false;
            }
        }

        if (proto.ComponentsBlacklist.Count > 0)
        {
            foreach (var componentType in proto.ComponentsBlacklist)
            {
                var type = Type.GetType(componentType);
                if (type != null && HasComp(uid, type))
                    return false;
            }
        }

        return true;
    }

    private bool TryGetRandomComment(MTGComponent mtgComp, out string comment)
    {
        comment = string.Empty;

        if (string.IsNullOrEmpty(mtgComp.CommentsDataset))
            return false;

        if (!_prototypeManager.TryIndex<LocalizedDatasetPrototype>(mtgComp.CommentsDataset, out var dataset))
            return false;

        comment = Loc.GetString(_random.Pick(dataset.Values));
        return true;
    }

    private void CompleteTest(EntityUid uid, string testId, ArchonDataComponent dataComp)
    {
        if (!dataComp.ActiveTests.Remove(testId))
            return;

        dataComp.CompletedTests.Add(testId);

        if (dataComp.MTG is not { } mtgUid || !TryComp<MTGComponent>(mtgUid, out var mtgComp))
            return;

        if (!_prototypeManager.TryIndex<ArchonTestPrototype>(testId, out var testProto))
            return;

        var paper = Spawn(mtgComp.MachineOutput, Transform(mtgUid).Coordinates);
        var content = new StringBuilder();
        content.AppendLine("=== ОТЧЁТ О ВЫПОЛНЕНИИ ТЕСТА ===");
        content.AppendLine();
        content.AppendLine($"Тест '{testId}' был успешно выполнен!");
        content.AppendLine();

        if (TryGetRandomComment(mtgComp, out var comment))
            content.AppendLine(comment);

        _paperSystem.SetContent(paper, content.ToString());
        Spawn(testProto.Award, Transform(mtgUid).Coordinates);

        if (dataComp.ActiveTests.Count == 0)
            CreateFinalReport(uid, mtgUid, mtgComp, dataComp);
    }

    private void CreateFinalReport(EntityUid uid, EntityUid mtgUid, MTGComponent mtgComp, ArchonDataComponent dataComp)
    {
        var paper = Spawn(mtgComp.MachineOutput, Transform(mtgUid).Coordinates);
        var content = new StringBuilder();
        content.AppendLine("=== ДАННЫЕ С ТЕСТОВ ===");
        content.AppendLine();

        if (TryComp<ArchonGenerateComponent>(uid, out var genComp) && genComp.AddedPrototypes != null)
        {
            foreach (var proto in genComp.AddedPrototypes)
            {
                content.AppendLine(proto.Desc);
            }
        }
        else if (!string.IsNullOrEmpty(dataComp.Description))
        {
            content.AppendLine(dataComp.Description);
        }

        if (TryGetRandomComment(mtgComp, out var comment))
            content.AppendLine(comment);

        _paperSystem.SetContent(paper, content.ToString());
        _audio.PlayPvs(mtgComp.SuccessSound, mtgUid);
        _chat.TrySendInGameICMessage(mtgUid, Loc.GetString("mtg-test-complete"), InGameICChatType.Speak, true);
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
