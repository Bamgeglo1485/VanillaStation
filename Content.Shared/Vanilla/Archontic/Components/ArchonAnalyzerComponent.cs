using Content.Shared.Containers.ItemSlots;
using Content.Shared.Archontic.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Стационарная штука, которая анализируют документы
/// </summary>
[RegisterComponent, Access(typeof(SharedArchonAnalyzeSystem))]
public sealed partial class ArchonAnalyzerComponent : Component
{

    /// <summary>
    /// Контейнер где вставляются архонты
    /// </summary>
    [DataField(required: true)]
    public ItemSlot ItemSlot = new();

    /// <summary>
    /// Привязанный Архонт
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedArchon;

}
