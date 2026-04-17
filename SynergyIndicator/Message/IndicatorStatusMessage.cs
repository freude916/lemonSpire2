using lemonSpire2.SynergyIndicator.Models;
using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using IndicatorType = lemonSpire2.SynergyIndicator.IndicatorRegistry.IndicatorType;

namespace lemonSpire2.SynergyIndicator.Message;

public record IndicatorStatusMessage : BasePlayerMessage
{
    public required IndicatorType IndicatorType { get; set; }
    public required IndicatorStatus Status { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteULong(SenderId);
        writer.WriteInt((int)IndicatorType);
        writer.WriteInt((int)Status);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        SenderId = reader.ReadULong();
        IndicatorType = (IndicatorType)reader.ReadInt();
        Status = (IndicatorStatus)reader.ReadInt();
    }
}
