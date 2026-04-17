using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public enum ChatNotificationSound
{
    None = 0,
    ReceiveMessage = 1,
    AtMessage = 2
}

public record ChatMessage : BasePlayerMessage
{
    public required IReadOnlyCollection<IMsgSegment> Segments { get; set; } = [];

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string? SenderName { get; set; } // Optional display name, for UI convenience

    public ulong ReceiverId { get; set; } // 0 = broadcast

    public ChatNotificationSound NotificationSound { get; set; } = ChatNotificationSound.None;

    public override void Serialize(PacketWriter writer)
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
        writer.WriteLong(Timestamp.ToUnixTimeSeconds());
    }

    public override void Deserialize(PacketReader reader)
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
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.ReadLong());
    }
}
