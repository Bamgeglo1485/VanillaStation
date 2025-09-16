using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Прототип с данными о возможных для добавления компонентов в архон. Чтобы компонент можно было разделить на классы и типы
/// </summary>
[Prototype("archonTest")]
public sealed partial class ArchonTestPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Описание эксперимента
    /// </summary>
    [DataField(required: true)]
    public string Desc = "Покакать";

    [DataField]
    public string Tag = "Generic";

    /// <summary>
    /// Прототип вознаграждения
    /// </summary>
    [DataField]
    public string Award = "AwardDisc5000";

    [DataField]
    public List<string> ComponentsWhitelist = new();

    [DataField]
    public List<string> ComponentsBlacklist = new();

    /// <summary>
    /// Минимальный уровень опасности для теста
    /// </summary>
    [DataField]
    public int MinDangerLevel = 0;

    /// <summary>
    /// Минимальный уровень побега для теста
    /// </summary>
    [DataField]
    public int MinEscapeLevel = 0;
}
