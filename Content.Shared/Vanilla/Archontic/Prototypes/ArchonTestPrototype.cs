using Content.Shared.Archontic.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Prototypes;

/// <summary>
/// Прототип теста архонта с двумя ответами
/// </summary>
[Prototype("archonTest")]
public sealed partial class ArchonTestPrototype : IPrototype
{

    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Описание вопроса
    /// </summary>
    [DataField(required: true)]
    public string QuestionDesc = "Объект выделяет фекалии?";

    /// <summary>
    /// Первый ответ
    /// </summary>
    [DataField(required: true)]
    public string FirstAnswer = "Да";

    /// <summary>
    /// Второй ответ
    /// </summary>
    [DataField(required: true)]
    public string SecondAnswer = "Нет";

    /// <summary>
    /// Условия первого ответа
    /// </summary>
    [DataField]
    public TestCondition FirstAnswerCondition = new();

    /// <summary>
    /// Условия второго ответа
    /// </summary>
    [DataField]
    public TestCondition SecondAnswerCondition = new();
}

[DataDefinition]
public partial struct TestCondition
{
    [DataField]
    public ComponentRegistry Whitelist { get; set; }

    [DataField]
    public ComponentRegistry Blacklist { get; set; }
}
