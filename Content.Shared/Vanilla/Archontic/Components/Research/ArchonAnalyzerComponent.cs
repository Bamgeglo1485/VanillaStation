using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Стационарная штука, которая анализируют документы
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ArchonAnalyzerComponent : Component
{

    /// <summary>
    /// Привязанный Архонт
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedArchon;

    /// <summary>
    /// Проверяемая бумага
    /// </summary>
    [ViewVariables]
    public EntityUid? Paper;

    /// <summary>
    /// Контейнер для документов
    /// </summary>
    [DataField]
    public string SlotId = new("paperSlot");

    /// <summary>
    /// Прототип бумаги отчёта
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "ArchonAnalyzerLogs";

    /// <summary>
    /// Прототип вознаграждения
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string AwardPrototype = "AwardDisc";

    /// <summary>
    /// Прототип синхронизированного документа
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DocumentPrototype = "SynchronizedDocument";

    /// <summary>
    /// Длительность анал лиза
    /// </summary>
    [DataField]
    public TimeSpan AnalyzeDelay = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Когда закончится анализ
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan AnalyzeEnd = TimeSpan.Zero;

    /// <summary>
    /// Привязанный Архонт
    /// </summary>
    [ViewVariables]
    public bool Analyzing;

    /// <summary>
    /// Звук ну типа удачно
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Vanilla/Items/archonLoad.ogg");

    /// <summary>
    /// Звук ну типо недуачно
    /// </summary>
    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/Vanilla/Items/archonCancel.ogg");

}
