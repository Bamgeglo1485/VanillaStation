using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
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
    public float AwakeChance = 0.4f;

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
    /// Длительность стазиса
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StasisDelay = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Сколько максимум можно получить снаряд ХИД
    /// </summary>
    [DataField]
    public int MaxHits = 3;

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
