using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.StatsTracker;

[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply), typeof(PowerModel), typeof(Creature), typeof(decimal),
    typeof(Creature), typeof(CardModel), typeof(bool))]
public static class PowerCmdPatch
{
    private static Logger Log => StatsTrackerManager.Log;

    public static void Postfix(PowerModel power, Creature target, decimal amount, Creature? applier,
        CardModel? cardSource, bool silent)
    {
        // Very unluckily the history are changed before the power apply,
        // So we have to use a ugly async continuation to collect after 1 frame

        // ProcessAfterApply(power, target, amount, applier).ContinueWith(_ => { });

        // check if this is needed
    }


    private static async Task ProcessAfterApply(PowerModel power, Creature target, decimal amount, Creature? applier)
    {
        // 等待一帧，确保 power.ApplyInternal 已经执行
        await Task.Yield();

        if (applier == null || amount <= 0) return;

        // 获取施加者的玩家
        var applierPlayer = applier.IsPlayer ? applier.Player : applier.PetOwner;
        if (applierPlayer == null)
        {
            Log.Info("Skipped: applierPlayer is null");
            return;
        }

        var stats = StatsTrackerManager.Instance.GetOrCreateStats(applierPlayer.NetId);

        // 施加者和目标是同一人 → 自身能力，暂不统计
        if (applier == target)
        {
            Log.Info("Skipped: applier == target (self-buff)");
            return;
        }

        // 根据目标类型统计
        var isTargetPlayer = target.IsPlayer || target.PetOwner != null;
        var intAmount = (int)amount;

        if (isTargetPlayer)
        {
            // 给队友上 buff
            if (power.Type == PowerType.Buff)
            {
                Log.Info($"Added buff: {intAmount}");
                stats.Add("stats.combat.buffs_applied", intAmount);
                stats.Add("stats.total.buffs_applied", intAmount);
            }
        }
        else
        {
            // 给敌人上 debuff
            if (power.Type == PowerType.Debuff)
            {
                Log.Info($"Added debuff: {intAmount}");
                stats.Add("stats.combat.debuffs_applied", intAmount);
                stats.Add("stats.total.debuffs_applied", intAmount);
            }
        }
    }
}
