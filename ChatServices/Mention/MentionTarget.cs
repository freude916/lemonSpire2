using lemonSpire2.Chat.Input.Service.Mention;
using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Input.Model;

public sealed record MentionTarget(string DisplayName, Func<IMsgSegment> CreateSegment)
{
    public string MentionText { get; init; } = MentionTextCodec.Encode(DisplayName);
}
