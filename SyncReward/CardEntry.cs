using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌条目
/// </summary>
public record CardEntry
{
    public string ModelId { get; set; } = "";
    public int UpgradeLevel { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(ModelId);
        writer.WriteInt(UpgradeLevel);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ModelId = reader.ReadString();
        UpgradeLevel = reader.ReadInt();
    }
}
