using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArchonDataComponent : Component
{

    /// <summary>
    /// Списан ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Expunged = false;

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
    /// Гуманоидный ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Humanoid = false;

    /// <summary>
    /// Уничтожаемость архона. Enum ArchonDestructibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArchonDestructibility Destructibility = new ();

    /// <summary>
    /// Синхронизированный документ
    /// </summary>
    [ViewVariables]
    public EntityUid? Document;

    /// <summary>
    /// К какому маяку привязан
    /// </summary>
    [ViewVariables]
    public EntityUid? Beacon;

    /// <summary>
    /// Список добавленных компонентов
    /// </summary>
    [DataField]
    public List<string> AddedComponents = new();

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

    /// <summary>
    /// Уровень опасности для класса Кетер
    /// </summary>
    [DataField]
    public int DangerLimit = 5;

    /// <summary>
    /// Уровень опасности для класса Евклид
    /// </summary>
    [DataField]
    public int EscapeLimit = 5;

    /// <summary>
    /// Ну логика объяснена в основном компоненте
    /// </summary>
    [DataField]
    public float RandomDangerMin;
    [DataField]
    public float RandomDangerMax;
    [DataField]
    public float RandomEscapeMin;
    [DataField]
    public float RandomEscapeMax;

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

public enum ArchonDestructibility : byte
{
    Normal, // Прочность от 10 до 200, если разумный стамина 100-200
    Hard, // От 300 до 700, стамина 400-800
    Invincible, // от 5000 до 10000
    Rebirth // После уничтожения перерождается с несколькими новыми компонентами, но после утрачивает эту способность
}
