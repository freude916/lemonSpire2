using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncShop;

/// <summary>
///     商店房间 Patch
///     在进入/离开商店时触发同步
/// </summary>
[HarmonyPatchCategory("ShopSync")]
[HarmonyPatch(typeof(NMerchantRoom))]
public static class ShopRoomPatch
{
    private static Logger Log => ShopNetworkHandler.Log;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMerchantRoom __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        Log.Debug("NMerchantRoom._Ready");
        // 使用 SceneTree 创建一个短暂延迟来确保 Inventory 已初始化
        var timer = __instance.GetTree().CreateTimer(0.1);
        timer.Timeout += ShopSynchronizer.SyncIfNeeded;
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePostfix()
    {
        Log.Debug("NMerchantRoom._ExitTree");
        ShopSynchronizer.BroadcastClearInventory();
    }
}
