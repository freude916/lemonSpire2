using System.Collections.ObjectModel;
using lemonSpire2.util;
using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.PlayerStateEx.Shop;

/// <summary>
///     商店库存同步消息
///     当玩家进入商店或库存变化时广播
/// </summary>
public record ShopInventoryMessage : BasePlayerMessage
{
    /// <summary>
    ///     商店物品列表
    /// </summary>
    public required Collection<ShopItemEntry> Items { get; set; } = [];

    /// <summary>
    ///     是否为清空消息（离开商店时发送）
    /// </summary>
    public bool IsClear { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteULong(SenderId);
        writer.WriteBool(IsClear);
        writer.WriteInt(Items.Count);
        foreach (var item in Items) item.Serialize(writer);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        SenderId = reader.ReadULong();
        IsClear = reader.ReadBool();
        var count = reader.ReadInt();
        Items = [];
        for (var i = 0; i < count; i++)
        {
            var entry = new ShopItemEntry();
            entry.Deserialize(reader);
            Items.Add(entry);
        }
    }
}

/// <summary>
///     商店物品条目
/// </summary>
public record ShopItemEntry
{
    public ShopItemType Type { get; set; }
    public string ModelId { get; set; } = "";
    public int Cost { get; set; }
    public bool IsStocked { get; set; }
    public bool IsOnSale { get; set; }
    public int UpgradeLevel { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteInt((int)Type);
        writer.WriteString(ModelId);
        writer.WriteInt(Cost);
        writer.WriteBool(IsStocked);
        writer.WriteBool(IsOnSale);
        writer.WriteInt(UpgradeLevel);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        Type = (ShopItemType)reader.ReadInt();
        ModelId = reader.ReadString();
        Cost = reader.ReadInt();
        IsStocked = reader.ReadBool();
        IsOnSale = reader.ReadBool();
        UpgradeLevel = reader.ReadInt();
    }
}

/// <summary>
///     商店物品类型
/// </summary>
public enum ShopItemType
{
    Card,
    Relic,
    Potion
}
