using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.PlayerStateEx.RewardEx;

/// <summary>
///     奖励来源类型
/// </summary>
public enum CardRewardSourceType
{
    /// <summary>
    ///     普通卡牌奖励（3选1）
    /// </summary>
    Normal,

    /// <summary>
    ///     特殊卡牌奖励（固定牌）
    /// </summary>
    Special,

    /// <summary>
    ///     被偷牌归还（不显示在面板）
    /// </summary>
    StolenBack
}

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

/// <summary>
///     卡牌奖励组
/// </summary>
public record CardRewardGroup
{
    public string GroupId { get; set; } = "";
    public CardRewardSourceType Source { get; set; }
    public List<CardEntry> Cards { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.Now;

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
