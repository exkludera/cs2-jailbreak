using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

public class CTQueue
{
    public static string QUEUE_PREFIX = "queue.queue_prefix";
    public static string JAILBREAK_PREFIX = "jailbreak.game_prefix";
    private readonly Queue<int> queueSlots = new Queue<int>();
    private readonly HashSet<int> queueSet = new HashSet<int>();
    // Track when players join CT team (slot -> timestamp)
    private readonly Dictionary<int, DateTime> ctJoinTimes = new Dictionary<int, DateTime>();
    // Flag to track if team balance is needed
    private bool needsRebalance = false;
    public JailConfig Config { get; set; } = new JailConfig();

    // Helper method to calculate the maximum number of CTs based on T count
    private int CalculateMaxCTs(int tCount, int ctCount = 0)
    {
        // Use floor division rather than ceiling to be more conservative
        // This ensures we maintain at least the configured team ratio
        int maxCTs = (int)Math.Floor((double)tCount / Config.Guard.TeamRatio);

        // Always allow at least 1 CT regardless of the ratio or player count
        // This ensures there's always at least one CT slot available
        return Math.Max(1, maxCTs);
    }

    // Helper method to check if a player is muted by an admin
    private bool IsPlayerAdminMuted(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return false;

        // Check if SimpleAdmin is enabled and available
        if (JB.JailPlugin.globalCtx?.SimpleAdminEnabled == true && JB.JailPlugin.globalCtx._SimpleAdminsharedApi != null)
        {
            var muteStatus = JB.JailPlugin.globalCtx._SimpleAdminsharedApi.GetPlayerMuteStatus(player);
            // If muteStatus is not null and has entries, player is muted by admin
            return muteStatus != null && muteStatus.Count > 0;
        }

        // If SimpleAdmin is not available, check if player has the muted flag
        return player.VoiceFlags.HasFlag(VoiceFlags.Muted);
    }

    public void Clear()
    {
        queueSlots.Clear();
        queueSet.Clear();
    }

    public bool IsInQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return false;

        return queueSet.Contains(player.Slot);
    }

    // Public method to track when a player joins CT team
    public void TrackCTJoin(CCSPlayerController? player)
    {
        if (player.IsLegal() && player.IsCt())
        {
            ctJoinTimes[player.Slot] = DateTime.UtcNow;
        }
    }

    public void JoinQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        // Check if player is CT banned using CTBans API
        if (JB.JailPlugin.globalCtx?.CTBansEnabled == true && JB.JailPlugin.globalCtx._CTBansApi != null)
        {
            if (JB.JailPlugin.globalCtx._CTBansApi.CheckAndNotifyPlayerCTBan(player))
            {
                // Player is CT banned, don't allow them to join the queue
                return;
            }
        }

        // Check if player is muted by an admin
        if (IsPlayerAdminMuted(player))
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.muted_cannot_join");
            return;
        }

        if (player.IsCt())
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.already_ct");
            return;
        }

        if (IsInQueue(player))
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.joined", queueSlots.Count);
            return;
        }

        queueSlots.Enqueue(player.Slot);
        queueSet.Add(player.Slot);

        int position = queueSlots.Count;
        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.joined", position);

        // Calculate estimated wait time based on queue position and team ratio
        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();
        int maxCTs = CalculateMaxCTs(tCount, ctCount);
        int availableSlots = maxCTs - ctCount;

        // If there are no CTs at all, process the queue immediately
        if (ctCount == 0 && tCount > 0)
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.processing_immediately");
            ProcessQueue();
            // End the current round and skip to the next one
            var GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
            Server.NextWorldUpdate(() =>
            {
                GameRules?.TerminateRound(0, RoundEndReason.RoundDraw);
            });
            return;
        }

        // Special case: If both teams are empty, allow the first player to join CT
        if (ctCount == 0 && tCount == 0)
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.processing_immediately");
            player.ChangeTeam(CsTeam.CounterTerrorist);
            Server.NextWorldUpdate(player.Respawn);

            Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_to_ct", player.PlayerName);

            // Track when this player joined CT
            ctJoinTimes[player.Slot] = DateTime.UtcNow;

            // Remove from queue since they're now on CT
            queueSlots.Dequeue();
            queueSet.Remove(player.Slot);
            return;
        }

        if (availableSlots > 0)
        {
            if (position <= availableSlots)
            {
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_next_round");
            }
            else
            {
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.slots_available", availableSlots);
            }
        }
        else
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.no_slots");
        }
    }

    public void LeaveQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (!IsInQueue(player))
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.not_in_queue");
            return;
        }

        // Need to rebuild the queue without this player
        RemovePlayerFromQueue(player.Slot);

        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.left");
    }

    private void RemovePlayerFromQueue(int slot)
    {
        if (!queueSet.Contains(slot))
            return;

        queueSet.Remove(slot);

        // Rebuild queue without this player
        var tempQueue = new Queue<int>();
        while (queueSlots.Count > 0)
        {
            int currentSlot = queueSlots.Dequeue();
            if (currentSlot != slot)
            {
                tempQueue.Enqueue(currentSlot);
            }
        }

        // Copy elements back to the original queue instead of reassigning
        queueSlots.Clear();
        foreach (var item in tempQueue)
        {
            queueSlots.Enqueue(item);
        }
    }

    public void ListQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (queueSlots.Count == 0)
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.empty");
            return;
        }

        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.current");

        int position = 1;
        foreach (int slot in queueSlots)
        {
            CCSPlayerController? queuedPlayer = Utilities.GetPlayerFromSlot(slot);
            if (queuedPlayer.IsLegal())
            {
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.position_entry", position, queuedPlayer.PlayerName);
            }
            position++;
        }
    }

    // Process the queue and move eligible players to CT team
    public void ProcessQueue(bool force = false)
    {
        if (!Config.Guard.Queue.Enabled || queueSlots.Count == 0)
            return;

        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();

        int potentialNextRoundTCount = Math.Max(1, tCount - 1);

        int maxCTsNow = CalculateMaxCTs(tCount);

        int basicAvailableSlots = Math.Max(0, maxCTsNow - ctCount);
        if (basicAvailableSlots <= 0 && !force)
            return;

        int queueSize = queueSlots.Count;
        int maxToMove = Math.Min(basicAvailableSlots, queueSize);

        int predictedTCount = tCount - maxToMove;
        int predictedCTCount = ctCount + maxToMove;

        int futureMaxCTs = CalculateMaxCTs(predictedTCount);

        int safeMaxToMove = maxToMove;
        if (predictedCTCount > futureMaxCTs)
        {
            safeMaxToMove = Math.Max(0, futureMaxCTs - ctCount);
        }

        int availableSlots = safeMaxToMove;

        if (availableSlots <= 0 && !force)
            return;

        // Process players in queue up to available slots
        int processed = 0;
        while (queueSlots.Count > 0 && (processed < availableSlots || force))
        {
            int slot = queueSlots.Peek();
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);

            if (!player.IsLegal() || player.IsCt())
            {
                // Remove invalid players or those already on CT
                queueSlots.Dequeue();
                queueSet.Remove(slot);
                continue;
            }

            // Check if player is CT banned using CTBans API
            if (JB.JailPlugin.globalCtx?.CTBansEnabled == true && JB.JailPlugin.globalCtx._CTBansApi != null)
            {
                if (JB.JailPlugin.globalCtx._CTBansApi.CheckAndNotifyPlayerCTBan(player))
                {
                    // Remove from queue since they're banned
                    queueSlots.Dequeue();
                    queueSet.Remove(slot);
                    continue;
                }
            }

            // Check if player is muted by an admin
            if (IsPlayerAdminMuted(player))
            {
                // Remove from queue and notify
                queueSlots.Dequeue();
                queueSet.Remove(slot);
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.muted_cannot_join");
                continue;
            }

            // Move player to CT
            player.ChangeTeam(CsTeam.CounterTerrorist);
            if (player.IsLegalAlive())
            {
                Server.NextWorldUpdate(player.Respawn);
            }
            Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_to_ct", player.PlayerName);

            // Track when this player joined CT
            ctJoinTimes[player.Slot] = DateTime.UtcNow;

            // Respawn the player after team switch to ensure they spawn in the armory
            if (JB.JailPlugin.globalCtx != null)
            {
                JB.JailPlugin.globalCtx.AddTimer(0.5f, () =>
                {
                    if (player.IsLegal() && player.PlayerPawn.IsValid && player.PlayerPawn.Value != null)
                    {
                        // Kill the player first to remove all weapons
                        player.PlayerPawn.Value.CommitSuicide(false, true);

                        // Then respawn them
                        player.Respawn();
                    }
                });
            }

            // Remove from queue
            queueSlots.Dequeue();
            queueSet.Remove(slot);
            processed++;

            // If we're not forcing, respect the available slots
            if (!force && processed >= availableSlots)
                break;
        }
    }

    // Check if team rebalancing is needed (when Ts leave)
    private void CheckTeamBalance()
    {
        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();

        // Calculate max allowed CTs based on current T count with strict ratio
        int maxCTs = CalculateMaxCTs(tCount, ctCount);

        // If we have more CTs than allowed, we need to rebalance
        // But always ensure we keep at least 1 CT
        if (ctCount > maxCTs && ctCount > 1)
        {
            needsRebalance = true;
        }
    }

    // Rebalance teams by moving newest CTs to T
    private void RebalanceTeams()
    {
        if (!needsRebalance)
            return;

        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();

        // Calculate max allowed CTs based on current T count with strict ratio
        int maxCTs = CalculateMaxCTs(tCount, ctCount);

        // If we have more CTs than allowed, move the newest ones to T
        // But always ensure we keep at least 1 CT
        if (ctCount > maxCTs && ctCount > 1)
        {
            // Calculate how many CTs to move while ensuring at least 1 CT remains
            int excessCTs = ctCount - Math.Max(1, maxCTs);

            // Get all CT players sorted by join time (newest first)
            var ctPlayers = new List<KeyValuePair<int, DateTime>>();
            foreach (var player in JB.Lib.GetPlayers())
            {
                if (player.IsCt() && ctJoinTimes.ContainsKey(player.Slot))
                {
                    ctPlayers.Add(new KeyValuePair<int, DateTime>(player.Slot, ctJoinTimes[player.Slot]));
                }
                else if (player.IsCt() && !ctJoinTimes.ContainsKey(player.Slot))
                {
                    // If we don't have a join time for this CT, add them with current time
                    // This ensures they'll be considered as newest CTs
                    ctJoinTimes[player.Slot] = DateTime.UtcNow;
                    ctPlayers.Add(new KeyValuePair<int, DateTime>(player.Slot, ctJoinTimes[player.Slot]));
                }
            }

            // Sort by join time (newest first)
            ctPlayers.Sort((a, b) => b.Value.CompareTo(a.Value));

            // Move the newest CTs to T and add them to front of queue
            int moved = 0;
            foreach (var ctPlayer in ctPlayers)
            {
                if (moved >= excessCTs)
                    break;

                CCSPlayerController? player = Utilities.GetPlayerFromSlot(ctPlayer.Key);
                if (player.IsLegal() && player.IsCt())
                {
                    player.ChangeTeam(CsTeam.Terrorist);
                    if (JB.JailPlugin.globalCtx != null)
                    {
                        JB.JailPlugin.globalCtx.AddTimer(0.5f, () =>
                        {
                            if (player.IsLegal() && player.PlayerPawn.IsValid && player.PlayerPawn.Value != null)
                            {
                                player.PlayerPawn.Value.CommitSuicide(false, true);
                                player.Respawn();
                            }
                        });
                    }
                    Chat.LocalizeAnnounce(JAILBREAK_PREFIX, "jailbreak.moved_to_t_balance", player.PlayerName);
                    ctJoinTimes.Remove(player.Slot);

                    // Add to front of queue if not already in queue
                    if (!IsInQueue(player))
                    {
                        var tempQueue = new Queue<int>();
                        tempQueue.Enqueue(player.Slot);
                        queueSet.Add(player.Slot);

                        // Add all existing queue members
                        while (queueSlots.Count > 0)
                        {
                            int slot = queueSlots.Dequeue();
                            tempQueue.Enqueue(slot);
                        }

                        // Replace the original queue with the new one
                        queueSlots.Clear();
                        while (tempQueue.Count > 0)
                        {
                            queueSlots.Enqueue(tempQueue.Dequeue());
                        }

                        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.added_to_front");
                    }

                    moved++;
                }
            }

            if (moved > 0)
            {
                Chat.LocalizeAnnounce(JAILBREAK_PREFIX, "jailbreak.team_rebalanced", moved);
            }
        }

        needsRebalance = false;
    }

    public void PlayerDisconnect(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);
        }

        // If a T player disconnects, check if we need to rebalance teams
        if (player.IsT())
        {
            CheckTeamBalance();
        }

        // Remove from CT join times if they were a CT
        if (player.IsCt() && ctJoinTimes.ContainsKey(player.Slot))
        {
            ctJoinTimes.Remove(player.Slot);
        }
    }

    public void RoundEnd()
    {
        // Check if we need to rebalance teams for the next round
        CheckTeamBalance();

        // Rebalance teams at round start if needed
        if (needsRebalance)
        {
            RebalanceTeams();
        }

        // Process queue
        ProcessQueue();
    }

    public void TeamChange(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        // Check if player is CT banned using CTBans API
        if (player.IsCt() && JB.JailPlugin.globalCtx?.CTBansEnabled == true && JB.JailPlugin.globalCtx._CTBansApi != null)
        {
            if (JB.JailPlugin.globalCtx._CTBansApi.CheckAndNotifyPlayerCTBan(player))
            {
                // Force them back to T team
                player.ChangeTeam(CsTeam.Terrorist);
                Server.NextWorldUpdate(player.Respawn);
                return;
            }
        }

        // If player is trying to join CT directly, check if they're muted
        if (player.IsCt() && IsPlayerAdminMuted(player))
        {
            // Force them back to T team
            player.ChangeTeam(CsTeam.Terrorist);
            Server.NextWorldUpdate(player.Respawn);
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.muted_cannot_join");
            return;
        }

        if (player.IsCt() && IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);

            // Track when this player joined CT
            ctJoinTimes[player.Slot] = DateTime.UtcNow;
        }
        else if (player.IsT() && ctJoinTimes.ContainsKey(player.Slot))
        {
            // Remove from CT join times if they switched to T
            ctJoinTimes.Remove(player.Slot);
        }

        Server.NextWorldUpdate(() =>
        {
            if (player.IsLegal() && player.PlayerPawn.IsValid && player.PlayerPawn.Value != null)
            {
                // Kill the player first to remove all weapons
                player.PlayerPawn.Value.CommitSuicide(false, true);

                // Then respawn them
                player.Respawn();
            }
        });

        if (player.IsT())
        {
            int ctCount = JB.Lib.CtCount();
            int tCount = JB.Lib.TCount();

            if (ctCount == 0 && tCount > 0)
            {
                // CT team is empty but there are still Ts, end the round
                Chat.LocalizeAnnounce(JAILBREAK_PREFIX, "jailbreak.ct_team_empty");

                // End the current round and skip to the next one
                var GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
                Server.NextWorldUpdate(() =>
                {
                    GameRules?.TerminateRound(0, RoundEndReason.RoundDraw);
                });
            }
        }

        CheckTeamBalance();
    }
}