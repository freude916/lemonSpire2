using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace lemonSpire2.util.Net;

/// <summary>
///     玩家消息基类
///     提供通用的 INetMessage 属性实现
/// </summary>
public abstract record BasePlayerMessage : INetMessage
{
    /// <summary>
    ///     发送者 NetId
    /// </summary>
    public required ulong SenderId { get; set; }

    public virtual bool ShouldBroadcast => true;
    public virtual NetTransferMode Mode => NetTransferMode.Reliable;
    public virtual LogLevel LogLevel => LogLevel.Debug;

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);
}
