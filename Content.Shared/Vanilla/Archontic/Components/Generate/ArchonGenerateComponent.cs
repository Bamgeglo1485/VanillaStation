using Content.Shared.Archontic.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.StoryGen;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonGenerateComponent : Component
{

    [DataField]
    public bool RandomType = true;

    [DataField]
    public bool RandomHealth = true;

    [DataField]
    public bool RandomDamage = true;

    [DataField]
    public bool RandomMovementSpeed = true;

    [DataField]
    public bool TriggerComponents = true;

    /// <summary>
    /// Тэги для подбора свойств
    /// </summary>
    [DataField(required: true)]
    public List<string> Tags;

    /// <summary>
    /// Тэги по которым выбирают свойства
    /// </summary>
    [DataField(required: true)]
    public List<string> AddingTags = new();

    /// <summary>
    /// Диапазон количества типов
    /// </summary>
    [DataField]
    public int MinTypes = 2;

    [DataField]
    public int MaxTypes = 3;

    /// <summary>
    /// Диапазон количества добавляемых компонентов
    /// </summary>
    [DataField]
    public int MinComponents = 2;

    [DataField]
    public int MaxComponents = 3;

    /// <summary>
    /// Список прототипов архонта
    /// </summary>
    [DataField]
    public List<ArchonComponentPrototype> AddedPrototypes = new();

    /// <summary>
    /// Используем систему рандомных историй из книг для описания пон
    /// </summary>
    [DataField]
    public ProtoId<StoryTemplatePrototype> Template;

}
