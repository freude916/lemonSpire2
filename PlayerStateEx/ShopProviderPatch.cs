using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     商店房间的 Harmony 补丁
///     监听普通商店生命周期，订阅原版库存的购买完成事件
/// </summary>
[HarmonyPatchCategory("ShopSync")]
[HarmonyPatch]
internal class ShopProviderPatch
{
    private static NMerchantRoom? _activeRoom;
    private static Logger Log => PlayerPanelRegistry.Log;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
    public static void ReadyPostfix(NMerchantRoom __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        Log.Debug("NMerchantRoom._Ready");

        if (__instance is not NStandardMerchantRoom || RunManager.Instance.State is not { } state)
            return;

        _activeRoom = __instance;
        ShopProvider.ObserveInventories(state.Players, RunManager.Instance.MerchantSynchronizer);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._ExitTree))]
    public static void ExitTreePostfix(NMerchantRoom __instance)
    {
        Log.Debug("NMerchantRoom._ExitTree");
        if (ReferenceEquals(_activeRoom, __instance))
            StopObserving();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MerchantSynchronizer), nameof(MerchantSynchronizer.OnMerchantExited))]
    public static void StopObserving()
    {
        _activeRoom = null;
        ShopProvider.StopObservingInventories();
    }
}
