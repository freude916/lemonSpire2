using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncShop;

/// <summary>
///     商店数据管理器
///     存储所有玩家的商店库存数据
/// </summary>
public class ShopManager
{
    /// <summary>
    ///     玩家 NetId -> 商店物品列表
    /// </summary>
    private readonly ConcurrentDictionary<ulong, Collection<ShopItemEntry>> _shopInventories = new();

    private ShopManager()
    {
    }

    private static Logger Log => ShopNetworkHandler.Log;

    public static ShopManager Instance { get; } = new();

    /// <summary>
    ///     商店库存更新事件，参数为更新的玩家 NetId
    /// </summary>
    public event Action<ulong>? InventoryUpdated;

    /// <summary>
    ///     更新玩家的商店库存
    /// </summary>
    public void UpdateInventory(ulong playerNetId, Collection<ShopItemEntry> items)
    {
        _shopInventories[playerNetId] = items ?? throw new ArgumentNullException(nameof(items));
        Log.Debug($"UpdateInventory: player={playerNetId}, items={items.Count}");
        InventoryUpdated?.Invoke(playerNetId);
    }

    /// <summary>
    ///     清除玩家的商店库存
    /// </summary>
    public void ClearInventory(ulong playerNetId)
    {
        _shopInventories.TryRemove(playerNetId, out _);
        Log.Debug($"ClearInventory: player={playerNetId}");
        InventoryUpdated?.Invoke(playerNetId);
    }

    /// <summary>
    ///     获取玩家的商店库存
    /// </summary>
    public Collection<ShopItemEntry>? GetInventory(ulong playerNetId)
    {
        return _shopInventories.GetValueOrDefault(playerNetId);
    }

    /// <summary>
    ///     检查玩家是否有商店数据
    /// </summary>
    public bool HasInventory(ulong playerNetId)
    {
        return _shopInventories.ContainsKey(playerNetId);
    }

    /// <summary>
    ///     从 MerchantInventory 创建 ShopItemEntry 列表
    /// </summary>
    public static Collection<ShopItemEntry> CreateEntriesFromInventory(MerchantInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var entries = new Collection<ShopItemEntry>();

        // 卡牌
        foreach (var cardEntry in inventory.CardEntries)
            if (cardEntry.CreationResult?.Card != null)
            {
                var card = cardEntry.CreationResult.Card;
                entries.Add(new ShopItemEntry
                {
                    Type = ShopItemType.Card,
                    ModelId = card.Id.Entry,
                    Cost = cardEntry.Cost,
                    IsStocked = cardEntry.IsStocked,
                    IsOnSale = cardEntry.IsOnSale,
                    UpgradeLevel = card.CurrentUpgradeLevel
                });
            }

        // 遗物
        foreach (var relicEntry in inventory.RelicEntries)
            if (relicEntry.Model != null)
                entries.Add(new ShopItemEntry
                {
                    Type = ShopItemType.Relic,
                    ModelId = relicEntry.Model.Id.Entry,
                    Cost = relicEntry.Cost,
                    IsStocked = relicEntry.IsStocked,
                    IsOnSale = false,
                    UpgradeLevel = 0
                });

        // 药水
        foreach (var potionEntry in inventory.PotionEntries)
            if (potionEntry.Model != null)
                entries.Add(new ShopItemEntry
                {
                    Type = ShopItemType.Potion,
                    ModelId = potionEntry.Model.Id.Entry,
                    Cost = potionEntry.Cost,
                    IsStocked = potionEntry.IsStocked,
                    IsOnSale = false,
                    UpgradeLevel = 0
                });

        return entries;
    }

    /// <summary>
    ///     检查本地玩家是否在商店房间
    /// </summary>
    public static bool IsLocalPlayerInShop()
    {
        return NMerchantRoom.Instance != null;
    }

    /// <summary>
    ///     重置数据（只清除数据，不创建新实例，保留事件订阅者）
    /// </summary>
    public static void Reset()
    {
        Instance._shopInventories.Clear();
        Log.Debug("Reset: cleared all inventories");
    }
}
