using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Intent;

public record IntentSendSegments : IIntent
{
    public ulong? SenderId { get; init; } // 0 for system, null to autofill, (also can be specified ? but shouldn't?
    public ulong? ReceiverId { get; init; } // 0 for broadcast, null to autofill
    public required IReadOnlyCollection<IMsgSegment> Segments { get; init; }
}
