using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     商店房间的 Harmony 补丁
///     监听 NMerchantRoom 生命周期事件，触发商店数据刷新
/// </summary>
[HarmonyPatchCategory("ShopSync")]
[HarmonyPatch(typeof(NMerchantRoom))]
internal class ShopProviderPatch
{
    private static SceneTreeTimer? _refreshTimer;
    private static Logger Log => PlayerPanelRegistry.Log;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMerchantRoom __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        Log.Debug("NMerchantRoom._Ready");

        ShopProvider.RequestRefresh();
        _refreshTimer = __instance.GetTree().CreateTimer(0.25);
        _refreshTimer.Timeout += RefreshLoop;
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePostfix()
    {
        Log.Debug("NMerchantRoom._ExitTree");
        _refreshTimer = null;

        // 通知所有当前面板：商店已离开，ShouldShow 将返回 false
        var room = NMerchantRoom.Instance?.Room;
        if (room != null)
            foreach (var inv in room.Inventories)
                ShopProvider.NotifyShopUpdated(inv.Player.NetId);
    }

    private static void RefreshLoop()
    {
        ShopProvider.RequestRefresh();

        var room = NMerchantRoom.Instance;
        if (room?.IsInsideTree() != true)
            return;

        _refreshTimer = room.GetTree().CreateTimer(0.25);
        _refreshTimer.Timeout += RefreshLoop;
    }
}
