using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using MySqlConnector;
using JB;
using CounterStrikeSharp.API.Modules.Entities;

public partial class Warden
{
    public Warden()
    {
        for (int p = 0; p < jailPlayers.Length; p++)
            jailPlayers[p] = new JailPlayer();

        // Initialize CTQueue
        ctQueue = new CTQueue();
    }

    public void SetWarden(int slot)
    {
        wardenSlot = slot;

        var player = Utilities.GetPlayerFromSlot(wardenSlot);

        // one last saftey check
        if (!player.IsLegal())
        {
            wardenSlot = INAVLID_SLOT;
            return;
        }

        Chat.LocalizeAnnounce(WARDEN_PREFIX, "warden.took_warden", player.PlayerName);

        player.LocalizeAnnounce(WARDEN_PREFIX, "warden.wcommand");

        wardenTimestamp = JB.Lib.CurTimestamp();

        // change player color!
        player.SetColour(Color.FromArgb(255, 0, 0, 255));

        JB.JailPlugin.logs.AddLocalized("warden.took_warden", player.PlayerName);

        wardenTime[slot] = DateTime.UtcNow;
    }

    public async void RemoveWarden()
    {
        var player = Utilities.GetPlayerFromSlot(wardenSlot);

        if (player.IsLegal())
        {
            player.SetColour(Player.DEFAULT_COLOUR);
            Chat.LocalizeAnnounce(WARDEN_PREFIX, "warden.removed", player.PlayerName);
            JB.JailPlugin.logs.AddLocalized("warden.removed", player.PlayerName);

            if (wardenTime.ContainsKey(player.Slot))
            {
                DateTime now = DateTime.UtcNow;
                int allSeconds = (int)Math.Round((now - wardenTime[player.Slot]).TotalSeconds);

                wardenTime.Clear();

                var authorizedid = player.AuthorizedSteamID;

                if (authorizedid == null)
                    return;

                await SaveWardenTime(authorizedid.SteamId64, allSeconds);
            }
        }

        RemoveWardenInternal();
    }

    void RemoveWardenInternal()
    {
        wardenSlot = INAVLID_SLOT;
        wardenTimestamp = -1;
    }


    public bool IsWarden(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return false;

        return player.Slot == wardenSlot;
    }

    public void RemoveIfWarden(CCSPlayerController? player)
    {
        if (IsWarden(player))
            RemoveWarden();
    }


    public bool PlayerChat(CCSPlayerController? player, CommandInfo info)
    {
        String text = info.GetArg(1);

        if (text.StartsWith("/") || text.StartsWith("!") || String.IsNullOrWhiteSpace(text))
            return true;

        if (player.IsLegalAlive() && IsWarden(player))
        {
            Server.PrintToChatAll($"{WARDEN_PREFIX} {player.PlayerName}: {text}");
            return false;
        }

        return true;
    }

    // reset variables for a new round
    void PurgeRound()
    {
        RemoveLaser();

        if (Config.Guard.Warden.ForceRemoval)
            RemoveWardenInternal();

        // reset player structs
        foreach (JailPlayer jailPlayer in jailPlayers)
            jailPlayer.PurgeRound();
    }

    void SetWardenIfLast(bool onDeath = false)
    {
        // dont override the warden if there is no death removal
        // also don't do it if an event is running because it's annoying
        if (!Config.Guard.Warden.ForceRemoval || JB.JailPlugin.EventActive())
            return;

        // if there is only one ct automatically give them warden!
        var ctPlayers = JB.Lib.GetAliveCt();

        if (ctPlayers.Count == 1)
        {
            int slot = ctPlayers[0].Slot;

            if (onDeath)
            {
                // play sfx for last ct
                // TODO: this is too loud as there is no way to control volume..
                //Lib.PlaySoundAll("sounds/vo/agents/sas/lastmanstanding03");
                var player = Utilities.GetPlayerFromSlot(slot);

                // Give last warden an extra bit of hp
                if (player.IsLegalAlive())
                    player.SetHealth(150);
            }

            SetWarden(slot);
        }
    }

    public void SetupPlayerGuns(CCSPlayerController? player)
    {
        // dont intefere with spawn guns if an event is running
        if (!player.IsLegalAlive() || JB.JailPlugin.EventActive())
            return;

        // strip weapons just in case
        if (Config.Settings.StripSpawnWeapons)
            player.StripWeapons();

        if (player.IsCt())
        {
            if (Config.Guard.Guns)
            {
                var jailPlayer = JailPlayerFromPlayer(player);

                player.GiveWeapon("deagle");

                if (jailPlayer != null)
                    player.GiveMenuWeapon(jailPlayer.ctGun);
            }

            if (Config.Guard.Armor)
                player.GiveArmor();
        }
    }

    // util func to get a jail player
    public JailPlayer? JailPlayerFromPlayer(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return null;

        return jailPlayers[player.Slot];
    }

    public CCSPlayerController? GetWarden()
    {
        if (wardenSlot == INAVLID_SLOT)
            return null;

        return Utilities.GetPlayerFromSlot(wardenSlot);
    }

    public static async Task SaveWardenTime(ulong steamID, int allSeconds)
    {
        if (allSeconds <= 0)
            return;

        var database = await JailPlugin.jailStats.ConnectDB();

        if (database == null)
            return;

        using var sql = new MySqlCommand($"UPDATE {JailPlugin.globalCtx.Config.Database.Table} SET wardentime = wardentime + {allSeconds} WHERE steamid = @steam_id", database);

        sql.Parameters.AddWithValue("@steam_id", steamID);

        await sql.ExecuteNonQueryAsync();
    }

    private static Dictionary<int, DateTime> wardenTime = new Dictionary<int, DateTime>();

    Countdown<int> chatCountdown = new Countdown<int>();

    CSTimer.Timer? tmpMuteTimer = null;
    long tmpMuteTimestamp = 0;

    const int INAVLID_SLOT = -3;

    int wardenSlot = INAVLID_SLOT;

    public static String WARDEN_PREFIX = $" {ChatColors.Green}[WARDEN]: {ChatColors.White}";

    long wardenTimestamp = -1;

    public JailConfig Config = new JailConfig();

    public JailPlayer[] jailPlayers = new JailPlayer[64];

    // slot for player for warden colour
    int colourSlot = -1;

    bool ctHandicap = false;

    public Warday warday = new Warday();
    public Block block = new Block();
    public Mute mute = new Mute();

    public CTQueue ctQueue = new CTQueue();
};