using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerStateEx.ShopEx;

/// <summary>
///     商店网络初始化 Patch
///     在 NGlobalUi.Initialize 时初始化网络处理器
/// </summary>
[HarmonyPatchCategory("ShopSync")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class ShopNetworkInitPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        ShopManager.Reset();
        ShopSynchronizer.Initialize(netService);
        MainFile.Logger.Info("[ShopPatch] ShopSynchronizer initialized");
    }
}

/// <summary>
///     商店房间 Patch
///     在进入/离开商店时触发同步
/// </summary>
[HarmonyPatchCategory("ShopSync")]
[HarmonyPatch(typeof(NMerchantRoom))]
public static class ShopRoomPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMerchantRoom __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        MainFile.Logger.Debug("[ShopPatch] NMerchantRoom._Ready");
        // 使用 SceneTree 创建一个短暂延迟来确保 Inventory 已初始化
        var timer = __instance.GetTree().CreateTimer(0.1);
        timer.Timeout += ShopSynchronizer.SyncIfNeeded;
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePostfix()
    {
        MainFile.Logger.Debug("[ShopPatch] NMerchantRoom._ExitTree");
        ShopSynchronizer.BroadcastClearInventory();
    }
}

/// <summary>
///     商店同步静态入口
/// </summary>
public static class ShopSynchronizer
{
    private static ShopNetworkHandler? _handler;

    public static void Initialize(INetGameService netService)
    {
        _handler?.Dispose();
        _handler = new ShopNetworkHandler(netService);
    }

    public static void SyncIfNeeded()
    {
        _handler?.SyncIfNeeded();
    }

    public static void BroadcastClearInventory()
    {
        _handler?.BroadcastClearInventory();
    }

    public static void Dispose()
    {
        _handler?.Dispose();
        _handler = null;
    }
}
