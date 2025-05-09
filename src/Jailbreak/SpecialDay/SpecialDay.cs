using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using JB;
using CounterStrikeSharp.API.Modules.Menu;

public enum SDState
{
    INACTIVE,
    STARTED,
    ACTIVE
};

// TODO: this will be done after lr and warden
// are in a decent state as its just an extra, and should 
// not take too long to port from css
public partial class SpecialDay
{
    private string CurrentSpecialDay = "none";

    public void EndSD(bool forced = false)
    {
        if (activeSD != null)
        {
            JailPlugin.EndEvent();
            activeSD.EndCommon();
            activeSD = null;
            countdown.Kill();

            Chat.LocalizeAnnounce(SPECIALDAY_PREFIX, JailPlugin.globalCtx.Localizer["specialday end", CurrentSpecialDay]);

            // restore all players if from a cancel
            if (forced)
            {
                foreach (CCSPlayerController player in JB.Lib.GetAliveCt())
                {
                    player.GiveWeapon("m4a1");
                    player.GiveWeapon("deagle");
                }

                Chat.Announce(SPECIALDAY_PREFIX, "Special day cancelled");
            }

            teamSave.Restore();
        }
    }

    public void SetupSD(CCSPlayerController? invoke, ChatMenuOption option)
    {
        if (!invoke.IsLegal())
            return;

        if (activeSD != null)
        {
            invoke.Announce(SPECIALDAY_PREFIX, "You cannot call two SD's at once");
            return;
        }

        // invoked as warden
        // reset the round counter so they can't do it again
        if (wsdCommand)
            wsdRound = 0;

        string name = option.Text!;

        switch (name)
        {
            case "Friendly fire":
                {
                    activeSD = new SDFriendlyFire();
                    type = SDType.FRIENDLY_FIRE;
                    CurrentSpecialDay = "Friendly fire";
                    break;
                }

            case "Juggernaut":
                {
                    activeSD = new SDJuggernaut();
                    type = SDType.JUGGERNAUT;
                    CurrentSpecialDay = "Juggernaut";
                    break;
                }

            case "Tank":
                {
                    activeSD = new SDTank();
                    type = SDType.TANK;
                    CurrentSpecialDay = "Tank";
                    break;
                }

            case "Scout knife":
                {
                    activeSD = new SDScoutKnife();
                    type = SDType.SCOUT_KNIFE;
                    CurrentSpecialDay = "Scout knife";
                    break;
                }

            case "Headshot only":
                {
                    activeSD = new SDHeadshotOnly();
                    type = SDType.HEADSHOT_ONLY;
                    CurrentSpecialDay = "Headshot only";
                    break;
                }

            case "Knife warday":
                {
                    activeSD = new SDKnifeWarday();
                    type = SDType.KNIFE_WARDAY;
                    CurrentSpecialDay = "Knife warday";
                    break;
                }

            case "Hide and seek":
                {
                    activeSD = new SDHideAndSeek();
                    type = SDType.HIDE_AND_SEEK;
                    CurrentSpecialDay = "Hide and seek";
                    break;
                }

            case "Dodgeball":
                {
                    activeSD = new SDDodgeball();
                    type = SDType.DODGEBALL;
                    CurrentSpecialDay = "Dodgeball";
                    break;
                }

            case "Spectre":
                {
                    activeSD = new SDSpectre();
                    type = SDType.SPECTRE;
                    CurrentSpecialDay = "Spectre";
                    break;
                }

            case "Grenade":
                {
                    activeSD = new SDGrenade();
                    type = SDType.GRENADE;
                    CurrentSpecialDay = "Grenade";
                    break;
                }

            case "Gun game":
                {
                    activeSD = new SDGunGame();
                    type = SDType.GUN_GAME;
                    CurrentSpecialDay = "Gun game";
                    break;
                }

            case "Zombie":
                {
                    activeSD = new SDZombie();
                    type = SDType.ZOMBIE;
                    CurrentSpecialDay = "Zombie";
                    break;
                }
        }

        // 1up dead players
        Lib.RespawnPlayers();

        Chat.LocalizeAnnounce(SPECIALDAY_PREFIX, JB.JailPlugin.globalCtx.Localizer["specialday start", CurrentSpecialDay]);

        // call the intiail sd setup
        if (activeSD != null)
        {
            teamSave.Save();

            JailPlugin.StartEvent();

            activeSD.delay = delay;
            activeSD.SetupCommon();

            // start the countdown for enable
            countdown.Start($"{name} starts in", delay, 0, null, StartSD);
        }
    }

    public void StartSD(int unused)
    {
        if (activeSD != null)
        {
            // force ff active
            if (overrideFF)
            {
                Chat.LocalizeAnnounce(SPECIALDAY_PREFIX, "sd.ffd_enable");
                Lib.EnableFriendlyFire();
            }

            activeSD.StartCommon();
        }
    }

    [RequiresPermissions("@css/generic")]
    public void CancelSDCmd(CCSPlayerController? player)
    {
        EndSD(true);
    }

    public void SDCmdInternal(CCSPlayerController? player)
    {
        if (!Config.SpecialDay.Enabled)
        {
            player.Announce(SPECIALDAY_PREFIX, "Special day is disabled!");
            return;
        }

        if (!player.IsLegal())
            return;

        delay = Config.SpecialDay.StartDelay;

        ChatMenu sdMenu = new($"Specialday");

        if (Config.SpecialDay.FriendlyFire) sdMenu.AddMenuOption("Friendly fire", SetupSD);
        if (Config.SpecialDay.Juggernaut) sdMenu.AddMenuOption("Juggernaut", SetupSD);
        if (Config.SpecialDay.Tank) sdMenu.AddMenuOption("Tank", SetupSD);
        if (Config.SpecialDay.Spectre) sdMenu.AddMenuOption("Spectre", SetupSD);
        if (Config.SpecialDay.Dodgeball) sdMenu.AddMenuOption("Dodgeball", SetupSD);
        if (Config.SpecialDay.Grenade) sdMenu.AddMenuOption("Grenade", SetupSD);
        if (Config.SpecialDay.ScoutKnife) sdMenu.AddMenuOption("Scout knife", SetupSD);
        if (Config.SpecialDay.HeadshotOnly) sdMenu.AddMenuOption("Hide and seek", SetupSD);
        if (Config.SpecialDay.HeadshotOnly) sdMenu.AddMenuOption("Headshot only", SetupSD);
        if (Config.SpecialDay.Knife) sdMenu.AddMenuOption("Knife warday", SetupSD);
        if (Config.SpecialDay.GunGame) sdMenu.AddMenuOption("Gun game", SetupSD);
        if (Config.SpecialDay.Zombie) sdMenu.AddMenuOption("Zombie", SetupSD);

        MenuManager.OpenChatMenu(player, sdMenu);
    }


    [RequiresPermissions("@jail/debug")]
    public void SDRigCmd(CCSPlayerController? player, CommandInfo command)
    {
        if (!player.IsLegal())
        {
            return;
        }

        if (activeSD != null && activeSD.state == SDState.STARTED)
        {
            player.PrintToChat($"Rigged sd boss to {player.PlayerName}");
            activeSD.riggedSlot = player.Slot;
        }
    }

    public void SDCmd(CCSPlayerController? player)
    {
        if (!player.IsGenericAdmin())
        {
            player.Announce(SPECIALDAY_PREFIX, "You must be an admin to use this command");
            return;
        }
        overrideFF = false;
        wsdCommand = false;
        SDCmdInternal(player);
    }

    public void SDFFCmd(CCSPlayerController? player)
    {
        if (!player.IsGenericAdmin())
        {
            player.Announce(SPECIALDAY_PREFIX, "You must be an admin to use this command");
            return;
        }
        overrideFF = true;
        wsdCommand = false;
        SDCmdInternal(player);
    }

    public void WardenSDCmdInternal(CCSPlayerController? player)
    {
        if (!JailPlugin.IsWarden(player))
        {
            player.Announce(SPECIALDAY_PREFIX, "You must be a warden to use this command");
            return;
        }

        // Not ready yet
        if (wsdRound < Config.SpecialDay.RoundsCooldown)
        {
            player.Announce(SPECIALDAY_PREFIX, $"Please wait {Config.SpecialDay.RoundsCooldown - wsdRound} more rounds");
            return;
        }

        // Go!
        wsdCommand = true;
        SDCmdInternal(player);
    }

    public void WardenSDCmd(CCSPlayerController? player)
    {
        overrideFF = false;

        WardenSDCmdInternal(player);
    }

    public void WardenSDFFCmd(CCSPlayerController? player)
    {
        overrideFF = true;

        WardenSDCmdInternal(player);
    }

    public enum SDType
    {
        FRIENDLY_FIRE,
        JUGGERNAUT,
        TANK,
        SPECTRE,
        DODGEBALL,
        GRENADE,
        SCOUT_KNIFE,
        HIDE_AND_SEEK,
        HEADSHOT_ONLY,
        KNIFE_WARDAY,
        GUN_GAME,
        ZOMBIE,
        NONE
    };

    public static string SPECIALDAY_PREFIX = $"  {ChatColors.Green}[Special day]: {ChatColors.White}";

    int delay = 15;

    public int wsdRound = 0;

    // NOTE: if we cared we would make this per player
    // so we can't get weird conflicts, but its not a big deal
    bool wsdCommand = false;

    SDBase? activeSD = null;

    bool overrideFF = false;

    Countdown<int> countdown = new Countdown<int>();

#pragma warning disable CS0414 // The field 'SpecialDay.type' is assigned but its value is never used
    SDType type = SDType.NONE;
#pragma warning restore CS0414 // The field 'SpecialDay.type' is assigned but its value is never used

    public JailConfig Config = new JailConfig();

    TeamSave teamSave = new TeamSave();
};