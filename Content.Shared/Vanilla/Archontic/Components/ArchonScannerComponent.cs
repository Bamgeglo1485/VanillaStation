using Content.Shared.Containers.ItemSlots;
using Content.Shared.Archontic.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Портативная штука, которая сканирует анхонты для дальнейшей привязки к оборудованиям
/// </summary>
[RegisterComponent, Access(typeof(SharedArchonSystem))]
public sealed partial class ArchonScannerComponent : Component
{

    /// <summary>
    /// Привязанный Архонт
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedArchon;

    /// <summary>
    /// Звук при скане архона
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Vanilla/Items/archonScan.ogg");

    /// <summary>
    /// Звук при передаче архона в анализатор
    /// </summary>
    [DataField]
    public SoundSpecifier? LoadSound = new SoundPathSpecifier("/Audio/Vanilla/Items/archonLoad.ogg");

}
