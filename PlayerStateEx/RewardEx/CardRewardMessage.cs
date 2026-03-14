using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.PlayerStateEx.RewardEx;

/// <summary>
///     卡牌奖励同步消息
///     当玩家获得卡牌奖励选择时广播
/// </summary>
public record CardRewardMessage : BasePlayerMessage
{
    /// <summary>
    ///     奖励组
    /// </summary>
    public required CardRewardGroup Group { get; set; }

    /// <summary>
    ///     是否为清空消息
    /// </summary>
    public bool IsClear { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteULong(SenderId);
        writer.WriteBool(IsClear);
        if (!IsClear) Group.Serialize(writer);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        SenderId = reader.ReadULong();
        IsClear = reader.ReadBool();
        if (!IsClear)
        {
            Group = new CardRewardGroup();
            Group.Deserialize(reader);
        }
    }
}
