using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Abstractions;

public interface IChatCompletionLeader
{
    char LeaderChar { get; }
    bool TryMatch(string text, int caretColumn, out ChatCompletionSession session);
}
