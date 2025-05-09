using CounterStrikeSharp.API.Core;

public class JailConfig : BasePluginConfig
{
    public DatabaseConfig Database { get; set; } = new DatabaseConfig();
    public SettingsConfig Settings { get; set; } = new SettingsConfig();
    public CTConfig Guard { get; set; } = new CTConfig();
    public TConfig Prisoner { get; set; } = new TConfig();
    public SpecialDayConfig SpecialDay { get; set; } = new SpecialDayConfig();
}

public class DatabaseConfig
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string IP { get; set; } = "127.0.0.1";
    public string Port { get; set; } = "3306";
    public string Database { get; set; } = "db";
    public string Table { get; set; } = "stats";
}

public class SettingsConfig
{
    public class SettingsCommands
    {
        public string WardenTime { get; set; } = "wtime,ctime";
        public string Kill { get; set; } = "kill,suicide";
    }
    public SettingsCommands Commands { get; set; } = new SettingsCommands();

    public class SettingsAdminCommands
    {
        public string RemoveWarden { get; set; } = "rw,rc,removewarden,removecommander";
        public string SwapGuard { get; set; } = "swapguard,swap_guard";
        public string FireGuard { get; set; } = "fire_guard";
        public string SpecialDay { get; set; } = "sd";
        public string SpecialDayFF { get; set; } = "sd_ff";
        public string SpecialDayCancel { get; set; } = "sd_cancel";
        public string Logs { get; set; } = "logs";
    }
    public SettingsAdminCommands AdminCommands { get; set; } = new SettingsAdminCommands();

    public bool NoBlock { get; set; } = true;
    public bool MuteDead { get; set; } = true;
    public bool StripSpawnWeapons { get; set; } = true;
    public bool WardayGuns { get; set; } = false;
    public bool RiotEnable { get; set; } = false;
    public bool HideKills { get; set; } = false;
    public bool RestrictPing { get; set; } = true;
}


public class CTConfig
{
    public class CTCommands
    {
        public string Guns { get; set; } = "guns";
    }
    public CTCommands Commands { get; set; } = new CTCommands();

    public int TeamRatio { get; set; } = 3;
    public bool SwapOnly { get; set; } = false;
    public bool Guns { get; set; } = true;
    public bool Handicap { get; set; } = false;
    public bool GunMenu { get; set; } = true;
    public bool Armor { get; set; } = true;
    public bool VoiceOnly { get; set; } = false;

    public class CTQueueConfig
    {
        public bool Enabled { get; set; } = true;
        public class QueueCommands
        {
            public string Join { get; set; } = "ctqueue,joinqueue,ct";
            public string Leave { get; set; } = "leavequeue";
            public string List { get; set; } = "listqueue,queuelist,queue";
        }
        public QueueCommands Commands { get; set; } = new QueueCommands();
    }
    public CTQueueConfig Queue { get; set; } = new CTQueueConfig();

    public class WardenConfig
    {
        public class WardenCommands
        {
            public string OpenMenu { get; set; } = "wmenu,cmenu";
            public string Warden { get; set; } = "w,c,warden,commander";
            public string UnWarden { get; set; } = "uw,uc,unwarden,uncommander";
            public string ListCommands { get; set; } = "wcommands,ccommands";
            public string RemoveMarker { get; set; } = "remove_marker";
            public string MarkerColor { get; set; } = "marker_color,marker_colour";
            public string LaserColor { get; set; } = "laser_color,laser_colour";
            public string HealPrisoners { get; set; } = "heal_t";
            public string NoBlock { get; set; } = "wb,wub,noblock";
            public string Color { get; set; } = "color,colour";
            public string SpecialDay { get; set; } = "wsd";
            public string SpecialDayFF { get; set; } = "wsd_ff";
            public string Warday { get; set; } = "wd,war,warday";
            public string Mute { get; set; } = "wm";
            public string GiveFreeday { get; set; } = "give_freeday";
            public string GivePardon { get; set; } = "give_pardon";
            public string Countdown { get; set; } = "countdown";
            public string CountdownAbort { get; set; } = "countdown_abort";
            public string ForceDoors { get; set; } = "force_open,force_close";
        }
        public WardenCommands Commands { get; set; } = new WardenCommands();

        public bool Laser { get; set; } = true;
        public bool OnVoice { get; set; } = true;
        public bool ForceRemoval { get; set; } = true;
    }
    public WardenConfig Warden { get; set; } = new WardenConfig();
}

public class TConfig
{
    public class TCommands
    {

    }
    public TCommands Commands { get; set; } = new TCommands();

    public bool ThirtySecMute { get; set; } = true;
    public bool MuteAlways { get; set; } = false;
    public bool RebelColor { get; set; } = true;
    public bool RebelAnnounce { get; set; } = true;

    public class LastRequestConfig
    {
        public class LRCommands
        {
            public string Start { get; set; } = "lr";
            public string Cancel { get; set; } = "cancel_lr";
            public string Stats { get; set; } = "lr_stats";
        }
        public LRCommands Commands { get; set; } = new LRCommands();

        public uint StartAliveCount { get; set; } = 2;
        public bool AllowRebel { get; set; } = false;
        public bool Knife { get; set; } = true;
        public bool GunToss { get; set; } = true;
        public bool Dodgeball { get; set; } = true;
        public bool NoScope { get; set; } = true;
        public bool War { get; set; } = true;
        public bool Grenade { get; set; } = true;
        public bool RussianRoulette { get; set; } = true;
        public bool ScoutKnife { get; set; } = true;
        public bool HeadshotOnly { get; set; } = true;
        public bool ShotForShot { get; set; } = true;
        public bool MagForMag { get; set; } = true;
    }
    public LastRequestConfig LR { get; set; } = new LastRequestConfig();
}

public class SpecialDayConfig
{
    public bool Enabled { get; set; } = true;
    public int StartDelay { get; set; } = 15;
    public int RoundsCooldown { get; set; } = 3;
    public bool Dodgeball { get; set; } = true;
    public bool FriendlyFire { get; set; } = true;
    public bool Grenade { get; set; } = true;
    public bool GunGame { get; set; } = true;
    public bool HeadshotOnly { get; set; } = true;
    public bool HideAndSeek { get; set; } = true;
    public bool Juggernaut { get; set; } = true;
    public bool Knife { get; set; } = true;
    public bool ScoutKnife { get; set; } = true;
    public bool Spectre { get; set; } = true;
    public bool Tank { get; set; } = true;
    public bool Zombie { get; set; } = true;
}