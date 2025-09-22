using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonGenerateComponent : Component
{

    [DataField]
    public bool RandomType = true;

    [DataField]
    public bool GenerateComponents = true;

    [DataField]
    public bool TriggerComponents = true;

    /// <summary>
    /// Ядро архонта
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ArchonCorePrototype> Core;

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
    /// Список добавленных компонентов
    /// </summary>
    [DataField]
    public List<string> AddedComponents = new();

    /// <summary>
    /// Список прототипов архонта
    /// </summary>
    [DataField]
    public List<ArchonComponentPrototype> AddedPrototypes = new();

}
