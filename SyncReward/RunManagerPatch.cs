using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncReward;

/// <summary>
///     房间切换 Patch
///     在进入新房间时清除所有人的卡牌奖励历史
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(RunManager))]
public static class RunManagerPatch
{
    private static Logger Log => CardRewardNetworkHandler.Log;

    [HarmonyPrefix]
    [HarmonyPatch("EnterRoom")]
    public static void EnterRoomPrefix()
    {
        // 进入新房间时清除所有人的历史
        CardRewardManager.Reset();
        Log.Debug("Cleared all card rewards on entering new room");
    }
}
