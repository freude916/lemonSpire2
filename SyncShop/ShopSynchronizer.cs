using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace lemonSpire2.SyncShop;

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
