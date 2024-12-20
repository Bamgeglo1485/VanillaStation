
using Robust.Shared.Configuration;
using Robust.Shared;

namespace Content.Shared.Vanilla.CCVars;

[CVarDefs]
public sealed class CCVarsVanilla : CVars
{

    /// <summary>
    /// URL вебхука, используемого для отправки сообщений о банах на Discord сервер.
    /// </summary>
    public static readonly CVarDef<string> DiscordServerBansWebhook =
        CVarDef.Create("discord.server_bans_webhook", string.Empty, CVar.SERVERONLY);
        
    /// <summary>
    /// задержка между вызовами обр
    /// </summary>
    public static readonly CVarDef<int> SpecForceDelay =
        CVarDef.Create("specforce.delay", 2, CVar.SERVERONLY);
}