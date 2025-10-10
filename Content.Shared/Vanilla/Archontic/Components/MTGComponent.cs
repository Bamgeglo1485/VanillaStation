using Content.Shared.Dataset;

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Генератор заурядных тестов, даёт рандомные тесты для архонта
/// </summary>

[RegisterComponent]
public sealed partial class MTGComponent : Component
{
    /// <summary>
    /// Максимум выдаваемых тестов
    /// </summary>
    [DataField]
    public int MaxTests = 4;

    /// <summary>
    /// Максимум перевыдач тестов, чтобы не спамили
    /// </summary>
    [DataField]
    public int MaxOutputs = 10;

    [DataField]
    public int Outputs = 0;

    /// <summary>
    /// Комментарий который будет печатать на отчётах ИИ ГЗТ, должен быть указан Датасет
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CommentsDataset { get; private set; }

    /// <summary>
    /// Прототип бумаги отчёта
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "ArchonAnalyzerLogs";

    /// <summary>
    /// Звук ну типа удачно
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Vanilla/Items/mtgSuccess.ogg");

    /// <summary>
    /// Звук ну типо недуачно
    /// </summary>
    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/Vanilla/Items/mtgDeny.ogg");
}
