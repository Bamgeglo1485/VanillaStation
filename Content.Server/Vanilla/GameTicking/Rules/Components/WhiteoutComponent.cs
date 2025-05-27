using Content.Server.Vanilla.GameTicking.Rules;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Weather;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(WhiteoutRuleSystem))]
public sealed partial class WhiteoutRuleComponent : Component
{

    [DataField]
    public float WhiteoutLength = 900;
    [DataField]
    public float WhiteoutFinalLength = 180f;
    [DataField]
    public float WhiteoutPrepareTime = 50f;

    [DataField]
    public float WhiteoutTemp = 123.15f;
    [DataField]
    public float WhiteoutFinalTemp = 23.15f;

    [DataField]
    public float WhiteoutStrength = 0.035f;
    [DataField]
    public float WhiteoutFinalModifier = 2f;

    [DataField]
    public string Weather = "SnowfallHeavy";

    [DataField]
    public string? WhiteoutPrepareAnnouncement = "whiteout-prepare-announcement";
    [DataField]
    public string? WhiteoutAnnouncement = "whiteout-announcement";
    [DataField]
    public string? WhiteoutFinalAnnouncement = "whiteout-announcement-final";
    [DataField]
    public string? WhiteoutEndAnnouncement = "whiteout-announcement-end";

    [DataField]
    public SoundSpecifier WhiteoutSoundAnnouncement = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/announcement.ogg");
    [DataField]
    public SoundSpecifier WhiteoutFinalSoundAnnouncement = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/whiteout_siren.ogg");

    [DataField]
    public SoundSpecifier WhiteoutMusic = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/whiteout.ogg");
    [DataField]
    public SoundSpecifier WhiteoutFinalMusic = new SoundPathSpecifier("/Audio/Vanilla/StationEvents/whiteout_final.ogg");

    public float TimeActive;
    public TimeSpan NextUpdate;
}
