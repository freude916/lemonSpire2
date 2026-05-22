using Godot;
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
    private static SceneTreeTimer? _refreshTimer;
    private static Logger Log => MainFile.Log;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMerchantRoom __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        Log.Debug("NMerchantRoom._Ready");

        RefreshFromCurrentRoom();
        _refreshTimer = __instance.GetTree().CreateTimer(0.25);
        _refreshTimer.Timeout += RefreshLoop;
    }

    [HarmonyPostfix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePostfix()
    {
        Log.Debug("NMerchantRoom._ExitTree");
        _refreshTimer = null;
        ShopManager.Instance.ClearAllInventories();
    }

    private static void RefreshLoop()
    {
        RefreshFromCurrentRoom();

        var room = NMerchantRoom.Instance;
        if (room?.IsInsideTree() != true)
            return;

        _refreshTimer = room.GetTree().CreateTimer(0.25);
        _refreshTimer.Timeout += RefreshLoop;
    }

    private static void RefreshFromCurrentRoom()
    {
        var room = NMerchantRoom.Instance?.Room;
        ShopManager.Instance.RefreshFromMerchantRoom(room);
    }
}
