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
    private void OnRoundEnded(RoundStartedEvent args)
    {
        RegisteredArchons.Clear();
        RegisteredNumbers.Clear();
    }



}
