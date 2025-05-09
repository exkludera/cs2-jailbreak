using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;

namespace JB;

public static class Lib
{
    static public bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    static public void InvokePlayerMenu(CCSPlayerController? invoke, String name,
        Action<CCSPlayerController, ChatMenuOption> callback, Func<CCSPlayerController?, bool> filter)
    {
        if (!invoke.IsLegal())
            return;

        ChatMenu menu = new("Warden Menu");

        foreach (var player in Lib.GetPlayers())
        {
            if (filter(player))
                menu.AddMenuOption(player.PlayerName, callback);
        }

        MenuManager.OpenChatMenu(invoke, menu);
    }

    public static void ColourMenu(CCSPlayerController? player, Action<CCSPlayerController, ChatMenuOption> callback, String name)
    {
        if (!player.IsLegal())
            return;

        ChatMenu colourMenu = new(name);

        foreach (var item in Lib.COLOUR_CONFIG_MAP)
            colourMenu.AddMenuOption(item.Key, callback);

        MenuManager.OpenChatMenu(player, colourMenu);
    }

    static public void PlaySoundAll(String sound)
    {
        foreach (CCSPlayerController? player in Lib.GetPlayers())
            player.PlaySound(sound);
    }

    static public void MuteT()
    {
        foreach (CCSPlayerController player in Lib.GetPlayers())
        {
            if (player.IsT() && !player.IsVip())
                player.Mute();
        }
    }

    static public void KillTimer(ref CSTimer.Timer? timer)
    {
        if (timer != null)
        {
            timer.Kill();
            timer = null;
        }
    }

    static public void UnMuteAll(bool roundEnd = false)
    {
        foreach (CCSPlayerController player in Lib.GetPlayers())
        {
            if (JailPlugin.globalCtx?.SimpleAdminEnabled == true && JailPlugin.globalCtx._SimpleAdminsharedApi != null)
            {
                var muteStatus = JailPlugin.globalCtx._SimpleAdminsharedApi.GetPlayerMuteStatus(player);
                if (muteStatus?.Count == 0)
                {
                    if (player.IsLegalAlive() || roundEnd)
                        player.UnMute();
                }
                else
                {
                    var muted_str = Chat.Localize("mute.muted");
                    var muted_prefix = Chat.Localize("mute.mute_prefix");
                    player.PrintToChat(muted_prefix + muted_str);
                }
            }

        }
    }

    static public long CurTimestamp()
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    static public void EnableFriendlyFire()
    {
        if (ff != null)
            ff.SetValue(true);
    }

    static public void DisableFriendlyFire()
    {
        if (ff != null)
            ff.SetValue(false);
    }

    static public void SwapAllT()
    {
        foreach (var player in GetAlivePlayers())
            player.SwitchTeam(CsTeam.Terrorist);
    }

    static public void SwapAllCT()
    {
        foreach (var player in GetAlivePlayers())
        {
            player.SwitchTeam(CsTeam.CounterTerrorist);

            // Track when this player joined CT
            if (JB.JailPlugin.warden.ctQueue != null)
            {
                JB.JailPlugin.warden.ctQueue.TrackCTJoin(player);
            }
        }
    }

    static public void RespawnPlayers()
    {
        // 1up all dead players
        foreach (CCSPlayerController player in Lib.GetActivePlayers())
        {
            if (!player.IsLegalAlive())
                player.Respawn();
        }
    }

    static public void ColorAllPlayerModels(Color color)
    {
        foreach (var prop in Utilities.GetAllEntities().Where(p => p.DesignerName.StartsWith("prop_dynamic")))
        {
            var model = prop.EntityHandle.Value!.As<CDynamicProp>();
            if (model.IsValid)
            {
                model.RenderMode = RenderMode_t.kRenderTransColor;
                model.Render = color;
                Utilities.SetStateChanged(model, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    static public List<CCSPlayerController> GetAlivePlayers()
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        return players.FindAll(player => player.IsLegalAlive());
    }

    static public List<CCSPlayerController> GetPlayers()
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        return players.FindAll(player => player.IsLegal() && player.IsConnected());
    }

    static public List<CCSPlayerController> GetAliveCt()
    {
        List<CCSPlayerController> players = Lib.GetPlayers();
        return players.FindAll(player => player.IsLegalAlive() && player.IsCt());
    }

    static public int CtCount()
    {
        List<CCSPlayerController> players = Lib.GetPlayers();
        return players.FindAll(player => player.IsLegal() && player.IsCt()).Count;
    }

    static public int TCount()
    {
        List<CCSPlayerController> players = Lib.GetPlayers();
        return players.FindAll(player => player.IsLegal() && player.IsT()).Count;
    }

    static public int AliveCtCount()
    {
        return GetAliveCt().Count;
    }

    static public List<CCSPlayerController> GetAliveT()
    {
        List<CCSPlayerController> players = Lib.GetPlayers();
        return players.FindAll(player => player.IsLegalAlive() && player.IsT()); ;
    }

    static public List<CCSPlayerController> GetActivePlayers()
    {
        List<CCSPlayerController> players = Lib.GetPlayers();
        return players.FindAll(player => player.IsT() || player.IsCt()); ;
    }

    static public int AliveTCount()
    {
        return GetAliveT().Count;
    }

    static public bool BlockEnabled()
    {
        if (blockCvar != null)
            return blockCvar.GetPrimitiveValue<int>() == 1;

        return true;
    }

    static public void BlockAll()
    {
        if (blockCvar != null)
            blockCvar.SetValue(1);
    }

    static public void UnBlockAll()
    {
        if (blockCvar != null)
            blockCvar.SetValue(0);
    }


    static public void SetCvarStr(String name, String value)
    {
        // why doesn't this work lol

        ConVar? cvar = ConVar.Find(name);

        if (cvar != null)
            cvar.StringValue = value;
    }

    static public bool IsActiveTeam(int team)
    {
        return (team == Player.TEAM_T || team == Player.TEAM_CT);
    }

    // TODO: just go with a simple print for now
    static public void log(String str)
    {
        Console.WriteLine($"[JAILBREAK]: {str}");
    }


    public static readonly Color WHITE = Color.FromArgb(255, 255, 255, 255);
    public static readonly Color CYAN = Color.FromArgb(255, 153, 255, 255);
    public static readonly Color RED = Color.FromArgb(255, 255, 0, 0);
    public static readonly Color INVIS = Color.FromArgb(0, 255, 255, 255);
    public static readonly Color GREEN = Color.FromArgb(255, 0, 191, 0);

    public static readonly Dictionary<string, Color> COLOUR_CONFIG_MAP = new Dictionary<string, Color>()
    {
        {"White",Lib.WHITE}, // white
        {"Cyan",Lib.CYAN}, // cyan
        {"Pink",Color.FromArgb(255,255,192,203)} , // pink
        {"Red",Lib.RED}, // red
        {"Purple",Color.FromArgb(255,118, 9, 186)}, // purple
        {"Grey",Color.FromArgb(255,66, 66, 66)}, // grey
        {"Green",GREEN}, // green
        {"Yellow",Color.FromArgb(255,255, 255, 0)} // yellow
    };

    public static readonly Vector VEC_ZERO = new Vector(0.0f, 0.0f, 0.0f);
    public static readonly QAngle ANGLE_ZERO = new QAngle(0.0f, 0.0f, 0.0f);

    static ConVar? blockCvar = ConVar.Find("mp_solid_teammates");
    static ConVar? ff = ConVar.Find("mp_teammates_are_enemies");

    public const int HITGROUP_HEAD = 0x1;
}