using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Roles.Components;
using Content.Shared.StoryGen;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Даёт архонту рандомную персону ну блин типо "денис 9 лет любитель пива"
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArchonBriefingComponent : BaseMindRoleComponent
{

    /// <summary>
    /// Используем систему рандомных историй из книг для генерации пон
    /// </summary>
    [DataField]
    public ProtoId<StoryTemplatePrototype> Template;

    [DataField]
    public string? Briefing;

}
