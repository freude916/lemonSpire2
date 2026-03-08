using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace lemonSpire2.Chat;

/// <summary>
///     聊天消息网络协议
/// </summary>
public struct ChatMessage : INetMessage, IPacketSerializable
{
    public ulong senderId;
    public string senderName;
    public string content;
    public long timestamp;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(senderId);
        writer.WriteString(senderName);
        writer.WriteString(content);
        writer.WriteLong(timestamp);
    }

    public void Deserialize(PacketReader reader)
    {
        senderId = reader.ReadULong();
        senderName = reader.ReadString();
        content = reader.ReadString();
        timestamp = reader.ReadLong();
    }
}