using lemonSpire2.util;
using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerStateEx.Shop;

/// <summary>
///     商店网络同步处理器
///     负责广播本地玩家的商店数据，接收其他玩家的商店数据
/// </summary>
public sealed class ShopNetworkHandler : NetworkHandlerBase<ShopInventoryMessage>
{
    public ShopNetworkHandler(INetGameService netService) : base(netService)
    {
        MainFile.Logger.Info("[ShopNetworkHandler] Initialized");
    }

    /// <summary>
    ///     广播本地玩家的商店库存
    /// </summary>
    public void BroadcastShopInventory()
    {
        var merchantRoom = NMerchantRoom.Instance;
        if (merchantRoom?.Room?.Inventory == null)
        {
            MainFile.Logger.Debug("[ShopNetworkHandler] No merchant room or inventory, skipping broadcast");
            return;
        }

        var entries = ShopManager.CreateEntriesFromInventory(merchantRoom.Room.Inventory);
        var message = new ShopInventoryMessage
        {
            SenderId = LocalPlayerId,
            Items = entries,
            IsClear = false
        };

        SendMessage(message);
        MainFile.Logger.Info($"[ShopNetworkHandler] Broadcasted shop inventory with {entries.Count} items");

        // 同时更新本地缓存
        ShopManager.Instance.UpdateInventory(LocalPlayerId, entries);
    }

    /// <summary>
    ///     广播清空消息（离开商店时）
    /// </summary>
    public void BroadcastClearInventory()
    {
        var message = new ShopInventoryMessage
        {
            SenderId = LocalPlayerId,
            Items = [],
            IsClear = true
        };

        SendMessage(message);
        MainFile.Logger.Debug("[ShopNetworkHandler] Broadcasted shop clear message");

        // 同时清除本地缓存
        ShopManager.Instance.ClearInventory(LocalPlayerId);
    }

    protected override void OnReceiveMessage(ShopInventoryMessage message, ulong senderId)
    {
        if (IsSelf(senderId)) return;

        MainFile.Logger.Debug(
            $"[ShopNetworkHandler] Received shop inventory from player {message.SenderId}, items={message.Items.Count}, isClear={message.IsClear}");

        if (message.IsClear)
        {
            ShopManager.Instance.ClearInventory(message.SenderId);
        }
        else
        {
            ShopManager.Instance.UpdateInventory(message.SenderId, message.Items);
        }
    }

    /// <summary>
    ///     手动触发同步（供外部调用）
    /// </summary>
    public void SyncIfNeeded()
    {
        if (ShopManager.IsLocalPlayerInShop())
            BroadcastShopInventory();
    }
}
