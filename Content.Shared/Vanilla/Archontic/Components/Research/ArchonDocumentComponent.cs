using Robust.Shared.Prototypes;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Документ Архонта, при уничтожении которого тот списывается
/// </summary>
[RegisterComponent]
public sealed partial class ArchonDocumentComponent : Component
{

    /// <summary>
    /// Синхронизированный архонт
    /// </summary>
    [ViewVariables]
    public EntityUid? Archon;

}
