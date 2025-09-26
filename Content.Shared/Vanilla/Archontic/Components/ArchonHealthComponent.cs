using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.Archontic.Components;

[RegisterComponent]
public sealed partial class ArchonHealthComponent : Component
{

    /// <summary>
    /// Я не понял как редактировать Destructible
    /// </summary>
    [DataField]
    public float Health = 0f;

    /// <summary>
    /// Эффект смерти
    /// </summary>
    [DataField]
    public string DeathEffect = "EffectArchonDeath";
}
