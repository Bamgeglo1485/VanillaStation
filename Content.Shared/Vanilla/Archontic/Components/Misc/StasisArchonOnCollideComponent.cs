using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Archontic.Components;

/// <summary>
/// Переводит архонт в состояние стазиса при попадании
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class StasisArchonOnCollideComponent : Component
{

    /// <summary>
    /// Сколько максимум раз можно застрелить 1 архонт
    /// </summary>
    [ViewVariables]
    public int MaxHits = 2;

    /// <summary>
    /// Урон по SyncLevel
    /// </summary>
    [ViewVariables]
    public int SyncLevelDamage = 0;

    /// <summary>
    /// Длительность стазиса
    /// </summary>
    [DataField]
    public TimeSpan StasisDelay = TimeSpan.FromSeconds(180);

}
