using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌奖励网络初始化 Patch
///     在 NGlobalUi.Initialize 时初始化网络处理器
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class CardRewardNetworkInitPatch
{
    private static Logger Log => CardRewardNetworkHandler.Log;

    [HarmonyPostfix]
    public static void Postfix()
    {
        var netService = RunManager.Instance.NetService;

        CardRewardManager.Reset();

        if (netService.Type.IsMultiplayer())
        {
            CardRewardSynchronizer.Initialize(netService);
            Log.Info("CardRewardSynchronizer initialized for multiplayer");
        }
        else
        {
            Log.Info("Single player mode, CardRewardManager reset");
        }
    }
}
