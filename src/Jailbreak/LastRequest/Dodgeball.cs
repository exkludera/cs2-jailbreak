using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

public class LRDodgeball : LRBase
{
    public LRDodgeball(LastRequest manager, LastRequest.LRType type, int LRSlot, int playerSlot, String choice) : base(manager, type, LRSlot, playerSlot, choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {
        weaponRestrict = "flashbang";

        if (player.IsLegalAlive())
        {
            player.SetHealth(1);

            player.GiveWeapon("flashbang");

            switch (choice)
            {
                case "Vanilla":
                    break;

                case "Low gravity":
                    player.SetGravity(0.6f);
                    break;
            }
        }
    }

    public override void PairActivate()
    {
        // Activate failsafe for both players to make it fair
        DelayFailSafe(35.0f);

        // If we have a partner, activate failsafe for them too
        partner?.DelayFailSafe(35.0f);
    }

    public override void PlayerHurt(int damage, int health, int hitgroup)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

        if (player.IsLegalAlive())
            player.Slay();
    }

    public override void GrenadeThrown()
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

        if (!failSafe)
            GiveLRNadeDelay(1.4f, "weapon_flashbang");

        // failsafe timer is up give a he grenade
        else
        {
            weaponRestrict = "hegrenade";
            GiveLRNadeDelay(1.4f, "weapon_hegrenade");
        }
    }

    public override void EntCreated(CEntityInstance entity)
    {
        entity.RemoveDelay(1.4f, "flashbang_projectile");
    }
}