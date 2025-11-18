using Robust.Shared.Utility;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Существо будет закрывать глаза, если в N радиусе есть объект с компонентом BlockMovementOnEyeContactComponent
/// </summary>
[RegisterComponent]
public sealed partial class AutoEyeClosingComponent : Component
{
    //момент времени в который глаза будут закрыты
    [ViewVariables]
    public TimeSpan CloseEyeTime;

    //момент времени в который глаза будут открыты
    [ViewVariables]
    public TimeSpan OpenEyeTime;


    //длительность открытых глаз (в секундах)
    [DataField]
    public float OpenDuration = 6f;

    [DataField]
    public float CloseDuration = 0.5f;

    [DataField]
    public float BaseCloseDuration = 0.5f;

    // Если чел близко к архонту то он моргает дольше
    [DataField]
    public float BaseCloseDurationInMelee = 2.5f;
}
