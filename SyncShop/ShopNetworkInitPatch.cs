using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncShop;

/// <summary>
///     商店网络初始化 Patch
///     在 NGlobalUi.Initialize 时初始化网络处理器
/// </summary>
[HarmonyPatchCategory("ShopSync")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class ShopNetworkInitPatch
{
    private static Logger Log => ShopNetworkHandler.Log;

    [HarmonyPostfix]
    public static void Postfix()
    {
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer())
        {
            Log.Debug("Not multiplayer, skipping ShopSynchronizer initialization");
            return;
        }

        ShopManager.Reset();
        ShopSynchronizer.Initialize(netService);
        Log.Info("ShopSynchronizer initialized");
    }
}
