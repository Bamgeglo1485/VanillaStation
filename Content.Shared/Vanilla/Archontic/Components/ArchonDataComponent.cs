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
    /// Списан ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Expunged = false;

    /// <summary>
    /// Описание объекта, используется при выполнении всех тестов, и если архонт не был сгенерирован
    /// </summary>
    [DataField]
    public string? Description;

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
    /// Состояние архонта. Enum ArchonState
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArchonState State = new();

    /// <summary>
    /// Лень объяснять, используется для первичных действий при переходе. Enum ArchonState
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArchonState LastState = new();

    /// <summary>
    /// Использует ли состояния архонта
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HaveStates = true;

    /// <summary>
    /// Шанс выйти в пробуждении при стимуляции
    /// </summary>
    [DataField]
    public float AwakeChance = 0.4f;

    /// <summary>
    /// Гуманоидный ли объект
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Humanoid = false;

    /// <summary>
    /// Созданы ли тесты для него
    /// </summary>
    [DataField]
    public bool TestsGenerated = false;

    /// <summary>
    /// Текущие тесты
    /// </summary>
    [DataField]
    public List<string> ActiveTests = new();

    [DataField]
    public HashSet<string> CompletedTests = new();

    /// <summary>
    /// Уничтожаемость архона. Enum ArchonDestructibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArchonDestructibility Destructibility = new ();

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
    /// К какому ГЗТ привязан
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? MTG;

    /// <summary>
    /// Энтити после полиморфа, то есть стазисный объект
    /// </summary>
    [ViewVariables]
    public EntityUid? PolymorphEntity;

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
    public int DangerLimit = 8;

    /// <summary>
    /// Уровень опасности для класса Евклид
    /// </summary>
    [DataField]
    public int EscapeLimit = 8;

    /// <summary>
    /// Прототип стазиса, должен быть прототипом полиморфа
    /// </summary>
    [DataField]
    public string StasisPrototype = "ArchonStasis";

    /// <summary>
    /// Длительность стазиса
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StasisDelay = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Сколько раз в него попадал ХИД снаряд
    /// </summary>
    [ViewVariables]
    public int StasisHits = 0;

    /// <summary>
    /// Когда он выйдет из стазиса
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan StasisExit = TimeSpan.Zero;

    /// <summary>
    /// Тэг базовых компонентов
    /// </summary>
    [DataField]
    public string GenericTag = "Generic";

    /// <summary>
    /// Дополнительный тэг
    /// </summary>
    [DataField]
    public string? AdditiveTag;

    /// <summary>
    /// Если объект попадёт в космос, то он вернётся и пробудится
    /// </summary>
    [DataField]
    public bool Comeback = true;

    /// <summary>
    /// Звук при камбеке
    /// </summary>
    [DataField]
    public SoundSpecifier? ComebackSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/archonComeback.ogg");

    /// <summary>
    /// Эффект смерти и пробуждения
    /// </summary>
    [DataField]
    public string AwakeEffect = "EffectArchonDeath";

    /// <summary>
    /// Можно ли написать на него документ
    /// </summary>
    [DataField]
    public bool CanBeAnalyzed = false;

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

public enum ArchonState : byte
{
    Stasis, // Объект никак не влияет на внешние факторы
    Basic, // Обычне состояние
    Awake // Пробуждение. Объект не может перейти в стазис и хп и стамина умножаются
}

public sealed class ArchonDeathEvent : EntityEventArgs
{
}
