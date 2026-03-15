using System.Collections.ObjectModel;
using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.SyncShop;

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
