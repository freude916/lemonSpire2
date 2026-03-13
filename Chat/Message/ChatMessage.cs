using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace lemonSpire2.Chat.Message;

public record ChatMessage : INetMessage
{
    public required IReadOnlyCollection<IMsgSegment> Segments { get; set; } = [];

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public required ulong SenderId { get; set; } // 0 = system

    public string? SenderName { get; set; } // Optional display name, for UI convenience

    public ulong ReceiverId { get; set; } // 0 = broadcast
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public bool ShouldBroadcast =>
        true; // Sts2 don't allow for Client->Client messages, so we broadcast everything and filter on the receiving end

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteInt(Segments.Count);
        foreach (var seg in Segments)
        {
            writer.WriteInt(SegmentTypes.ToId(seg));
            seg.Serialize(writer);
        }

        writer.WriteULong(SenderId);
        writer.WriteString(SenderName ?? "");
        writer.WriteULong(ReceiverId);
        writer.WriteLong(Timestamp.Ticks);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var count = reader.ReadInt();
        var segments = new List<IMsgSegment>(count);
        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadInt();
            if (!SegmentTypes.TryGetType(id, out var type))
                throw new InvalidOperationException($"Unknown segment type id: {id}");

            var segment = (IMsgSegment)Activator.CreateInstance(type!)!; // TryGetType should be false if type is null
            segment.Deserialize(reader);
            segments.Add(segment);
        }

        Segments = segments;
        SenderId = reader.ReadULong();
        var name = reader.ReadString();
        SenderName = string.IsNullOrEmpty(name) ? null : name;
        ReceiverId = reader.ReadULong();
        Timestamp = new DateTime(reader.ReadLong());
    }
}
