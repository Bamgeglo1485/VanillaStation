using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ArchonComponent : Component
{

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
    /// Шанс выйти в пробуждении при стимуляции
    /// </summary>
    [DataField]
    public float AwakeChance = 0.5f;

    /// <summary>
    /// Базовая степень синхронизации, не меняющаяся от стазиса или пробуждения
    /// </summary>
    [DataField]
    public int BaseSyncLevel = 2;

    /// <summary>
    /// До какого уровня максимум дойдёт через время
    /// </summary>
    [DataField]
    public int PeakSyncLevel = 5;

    /// <summary>
    /// Текущая степень синхронизации
    /// </summary>
    [DataField]
    public int SyncLevel = 0;

    /// <summary>
    /// Максимальный уровень синхрона
    /// </summary>
    [DataField]
    public int MaxSyncLevel = 10;

    /// <summary>
    /// Скрытые компоненты
    /// </summary>
    [DataField]
    public List<SecretFeatures>? SecretFeatures { get; set; }

    /// <summary>
    /// За сколько времени восстановится 1 уровень синхронизации
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SyncLevelRecoverDelay = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Следующее обновление добавления уровня
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextSyncLevelRecover = TimeSpan.Zero;

    /// <summary>
    /// Энтити после полиморфа, то есть стазисный объект
    /// </summary>
    [ViewVariables]
    public EntityUid? PolymorphEntity;

    /// <summary>
    /// Прототип стазиса, должен быть прототипом полиморфа
    /// </summary>
    [DataField]
    public string StasisPrototype = "ArchonStasis";

    /// <summary>
    /// Длительность стазиса при спавне
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SpawnStasisDelay = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Длительность стазиса при 0 синхронизации
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StasisDelay = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Сколько раз он попадал в стазис
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
    /// Если объект попадёт в космос, то он вернётся и пробудится
    /// </summary>
    [DataField]
    public bool Comeback = true;

    /// <summary>
    /// Перерождается ли объект
    /// </summary>
    [DataField]
    public bool Rebirth = false;

    /// <summary>
    /// Звук при камбеке
    /// </summary>
    [DataField]
    public SoundSpecifier? ComebackSound = new SoundPathSpecifier("/Audio/Vanilla/Effects/archonComeback.ogg");

    /// <summary>
    /// Эффект пробуждения
    /// </summary>
    [DataField]
    public string AwakeEffect = "EffectArchonDeath";

    /// <summary>
    /// Сообщит о местоположении, если архонт создан на станции
    /// </summary>
    [DataField]
    public bool Announcement = true;

    /// <summary>
    /// Проиграл ли он уже оповещение
    /// </summary>
    [DataField]
    public bool AnnouncementPlayed = false;
}

[DataDefinition]
public partial class SecretFeatures
{
    [DataField]
    public ComponentRegistry? Components { get; set; }

    [DataField]
    public int RevealThreshold { get; set; }

    [DataField]
    public int Danger { get; set; }

    [DataField]
    public int Escape { get; set; }

    [DataField]
    public bool Revealed { get; set; }

    public SecretFeatures()
    {
        Components = new ComponentRegistry();
        RevealThreshold = 5;
        Danger = 0;
        Escape = 0;
        Revealed = false;
    }

    public SecretFeatures(ComponentRegistry components, int revealThreshold, int danger, int escape, bool revealed = false)
    {
        Components = components;
        RevealThreshold = revealThreshold;
        Danger = danger;
        Escape = escape;
        Revealed = revealed;
    }
}
