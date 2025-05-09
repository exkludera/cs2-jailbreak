using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;

// debugging commands:
// TODO: these need to be sealed by admin
public static class Debug
{
    [RequiresPermissions("@jail/debug")]
    public static void Nuke(CCSPlayerController? invoke, CommandInfo command)
    {
        Chat.Announce(DEBUG_PREFIX, "Slaying all players");

        Chat.PrintConsoleAll("Nuke!");

        Player.Nuke();
    }

    [RequiresPermissions("@jail/debug")]
    public static void TestLaser(CCSPlayerController? invoke, CommandInfo command)
    {
        CCSPlayerPawn? pawn = invoke.Pawn();

        if (pawn != null && pawn.AbsOrigin != null)
        {
            Circle marker = new Circle();

            marker.Draw(30.0f, 72.0f, pawn.AbsOrigin, System.Drawing.Color.Fuchsia);
        }
    }

    // block   0b000000000001011000011000000010001
    // noblock 0b100000000001011000011000000010001;

    [RequiresPermissions("@jail/debug")]
    public static void TestNoblockCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        if (!invoke.IsLegal())
            return;

        invoke.PrintToChat("changed collision");

        foreach (CCSPlayerController player in JB.Lib.GetPlayers())
        {
            var pawn = player.Pawn();

            if (pawn == null)
                continue;

            // TODO
        }
    }

    [RequiresPermissions("@jail/debug")]
    public static void TestStripCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        invoke.StripWeapons(true);
    }

    [RequiresPermissions("@jail/debug")]
    public static void JoinCtCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        if (invoke != null && invoke.IsLegal())
        {
            invoke.SwitchTeam(CsTeam.CounterTerrorist);

            // Track when this player joined CT
            if (JB.JailPlugin.warden.ctQueue != null)
            {
                JB.JailPlugin.warden.ctQueue.TrackCTJoin(invoke);
            }
        }
    }

    [RequiresPermissions("@jail/debug")]
    public static void HideWeaponCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        if (invoke != null && invoke.IsLegal())
            invoke.PrintToChat("hiding weapons");

        invoke.HideWeapon();
    }

    [RequiresPermissions("@jail/debug")]
    public static void WSDEnableCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        if (invoke != null && invoke.IsLegal())
        {
            invoke.PrintToChat("enable wsd");
            JB.JailPlugin.sd.wsdRound = 0x7000_0000;
        }
    }

    [RequiresPermissions("@jail/debug")]
    public static void IsMutedCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        if (!invoke.IsLegal())
            return;

        invoke.PrintToConsole("Is muted?");

        foreach (CCSPlayerController player in JB.Lib.GetPlayers())
            invoke.PrintToConsole($"{player.PlayerName} : {player.VoiceFlags.HasFlag(VoiceFlags.Muted)} : {player.VoiceFlags.HasFlag(VoiceFlags.ListenAll)} : {player.VoiceFlags.HasFlag(VoiceFlags.ListenTeam)}");
    }

    [RequiresPermissions("@jail/debug")]
    public static void TestLRInc(CCSPlayerController? invoke, CommandInfo command)
    {
        JB.JailPlugin.WinLR(invoke, LastRequest.LRType.KNIFE);
    }

    [RequiresPermissions("@jail/debug")]
    public static void TestPlayer(CCSPlayerController? invoke, CommandInfo command)
    {
        if (invoke.IsLegal())
        {
            var pawn = invoke.Pawn();

            if (pawn != null)
                invoke.PrintToChat($"name: {invoke.DesignerName} : {pawn.DesignerName}");
        }
    }

    // are these commands allowed or not?
    public static readonly bool enable = true;

    public static readonly String DEBUG_PREFIX = $" {ChatColors.Green}[DEBUG]: {ChatColors.White}";
}