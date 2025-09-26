using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Prototypes;

/// <summary>
/// Прототип со свойствами архонта
/// </summary>
[Prototype("archonFeature")]
public sealed partial class ArchonFeaturePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Тэги для разделения компонентов
    /// </summary>
    [DataField]
    public List<string> Tag = "Generic";

    /// <summary>
    /// Описание действия
    /// </summary>
    [DataField(required: true)]
    public string Desc = "Какает";

    /// <summary>
    /// Заканчивает подбор компонентов с шансом после выбора одного
    /// </summary>
    [DataField]
    public bool BreakAfterChance = true;

    /// <summary>
    /// Тип архонта для меньшей хаотичности. Enum ArchonType
    /// </summary>
    [DataField]
    public List<ArchonType> Types = new();

    /// <summary>
    /// Сами свойства
    /// </summary>
    [DataField("specials", serverOnly: true)]
    public ArchonSpecial[] Specials { get; private set; } = Array.Empty<ArchonSpecial>();
}
