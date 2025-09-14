using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Генератор заурядных тестов, даёт рандомные тесты для архонта
/// </summary>

[RegisterComponent]
public sealed partial class MTGComponent : Component
{

    [DataField]
    public int MaxTests = 3;

    [DataField]
    public string AwardDisc = "AwardDisc5000";

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
