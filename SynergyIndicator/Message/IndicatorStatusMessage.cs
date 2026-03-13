using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace lemonSpire2.SynergyIndicator.Message;

public record IndicatorStatusMessage : INetMessage
{
    public required ulong SenderId { get; set; }
    public required IndicatorType IndicatorType { get; set; }
    public required IndicatorStatus Status { get; set; }

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteULong(SenderId);
        writer.WriteInt((int)IndicatorType);
        writer.WriteInt((int)Status);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        SenderId = reader.ReadULong();
        IndicatorType = (IndicatorType)reader.ReadInt();
        Status = (IndicatorStatus)reader.ReadInt();
    }
}
