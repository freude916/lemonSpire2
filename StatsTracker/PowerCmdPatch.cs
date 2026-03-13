using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.StatsTracker;

[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply), typeof(PowerModel), typeof(Creature), typeof(decimal),
    typeof(Creature), typeof(CardModel), typeof(bool))]
public static class PowerCmdPatch
{
    public static void Postfix(PowerModel power, Creature target, decimal amount, Creature? applier,
        CardModel? cardSource, bool silent)
    {
        // Very unluckily the history are changed before the power apply,
        // So we have to use a ugly async continuation to collect after 1 frame
        ProcessAfterApply(power, target, amount, applier).ContinueWith(_ => { });
    }


    private static async Task ProcessAfterApply(PowerModel power, Creature target, decimal amount, Creature? applier)
    {
        // 等待一帧，确保 power.ApplyInternal 已经执行
        await Task.Yield();

        if (power == null || target == null || applier == null || amount <= 0) return;

        // 获取施加者的玩家
        var applierPlayer = applier.IsPlayer ? applier.Player : applier.PetOwner;
        if (applierPlayer == null)
        {
            MainFile.Logger.Info("[PowerCmdPatch] Skipped: applierPlayer is null");
            return;
        }

        var stats = StatsTrackerManager.Instance.GetOrCreateStats(applierPlayer.NetId);

        // 施加者和目标是同一人 → 自身能力，暂不统计
        if (applier == target)
        {
            MainFile.Logger.Info("[PowerCmdPatch] Skipped: applier == target (self-buff)");
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
                MainFile.Logger.Info($"[PowerCmdPatch] Added buff: {intAmount}");
                stats.Add("stats.combat.buffs_applied", intAmount);
                stats.Add("stats.total.buffs_applied", intAmount);
            }
        }
        else
        {
            // 给敌人上 debuff
            if (power.Type == PowerType.Debuff)
            {
                MainFile.Logger.Info($"[PowerCmdPatch] Added debuff: {intAmount}");
                stats.Add("stats.combat.debuffs_applied", intAmount);
                stats.Add("stats.total.debuffs_applied", intAmount);
            }
        }
    }
}
