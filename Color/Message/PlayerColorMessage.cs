using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.PlayerColor;

/// <summary>
///     玩家颜色同步消息
/// </summary>
public record PlayerColorMessage : BasePlayerMessage
{
    /// <summary>
    ///     目标玩家 ID（如果要改变其他玩家的颜色）
    ///     如果为 0，则表示改变自己的颜色
    /// </summary>
    public ulong TargetPlayerId { get; set; }

    /// <summary>
    ///     颜色 R 分量 (0-1)
    /// </summary>
    public float R { get; set; }

    /// <summary>
    ///     颜色 G 分量 (0-1)
    /// </summary>
    public float G { get; set; }

    /// <summary>
    ///     颜色 B 分量 (0-1)
    /// </summary>
    public float B { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteULong(SenderId);
        writer.WriteULong(TargetPlayerId);
        writer.WriteFloat(R);
        writer.WriteFloat(G);
        writer.WriteFloat(B);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        SenderId = reader.ReadULong();
        TargetPlayerId = reader.ReadULong();
        R = reader.ReadFloat();
        G = reader.ReadFloat();
        B = reader.ReadFloat();
    }
}
