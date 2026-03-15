using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.SyncShop;

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
