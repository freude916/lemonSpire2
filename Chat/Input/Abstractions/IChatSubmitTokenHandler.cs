using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Input.Abstractions;

public interface IChatSubmitTokenHandler
{
    char TriggerChar { get; }
    bool TryParse(string text, int startIndex, out IMsgSegment segment, out int length);
}
