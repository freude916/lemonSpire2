using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌奖励组
/// </summary>
public record CardRewardGroup
{
    public string GroupId { get; set; } = "";
    public CardRewardSourceType Source { get; set; }
    public List<CardEntry> Cards { get; set; } = [];
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(GroupId);
        writer.WriteInt((int)Source);
        writer.WriteInt(Cards.Count);
        foreach (var card in Cards) card.Serialize(writer);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        GroupId = reader.ReadString();
        Source = (CardRewardSourceType)reader.ReadInt();
        var count = reader.ReadInt();
        Cards = [];
        for (var i = 0; i < count; i++)
        {
            var entry = new CardEntry();
            entry.Deserialize(reader);
            Cards.Add(entry);
        }
    }
}
