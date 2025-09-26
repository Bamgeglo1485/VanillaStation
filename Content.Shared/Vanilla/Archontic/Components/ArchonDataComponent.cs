using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ArchonDataComponent : Component
{

    /// <summary>
    /// Активен ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;

    /// <summary>
    /// Класс архона, его опасность и возможность сбежать. Enum ArchonClass
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArchonClass Class = ArchonClass.Safe;

    /// <summary>
    /// Тип архона для меньшей хаотичности. Enum ArchonType
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ArchonType> Types = new();

    /// <summary>
    /// Разумен ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Sentient = false;

    /// <summary>
    /// Перерождается ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Rebirth = false;

    /// <summary>
    /// Синхронизированный документ
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Document;

    /// <summary>
    /// К какому маяку привязан
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Beacon;

    /// <summary>
    /// Уровень опасности
    /// </summary>
    [DataField]
    public int Danger = 0;

    /// <summary>
    /// Уровень опасности
    /// </summary>
    [DataField]
    public int Escape = 0;

}

public enum ArchonClass : byte
{
    Safe, // Если объект можно безопасно содержать, и оно не будет пытаться вырваться или приносить вред.
    Euclid, // Если объект не будет сбегать с коробки, но будет приносить пассивный вред или потенциальный.
    Keter, // Если объект может сбежать, но не обязательно приносить вред.
    Thaumiel // Если объект и приносит вред, и сбегает.
}

public enum ArchonType : byte
{
    Hylic = 0, // Излучение
    Pneumatic = 1, // Газы, движение
    Luminary = 2, // Разум
    Demiurge = 3, // Создание
    Archon = 4 // Прочее
}

public enum ArchonState : byte
{
    Stasis, // Объект никак не влияет на внешние факторы
    Basic, // Обычне состояние
    Awake // Пробуждение. Объект не может перейти в стазис и хп и стамина умножаются
}

public sealed class ArchonDeathEvent : EntityEventArgs
{
}
