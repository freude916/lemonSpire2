using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Service.Bracket;

public sealed class BracketCompletionLeader(IChatCompletionProvider provider) : IChatCompletionLeader
{
    public char LeaderChar => '<';

    public bool TryMatch(string text, int caretColumn, out ChatCompletionSession session)
    {
        ArgumentNullException.ThrowIfNull(text);
        session = null!;
        var leaderIndex = text.LastIndexOf('<', caretColumn - 1, caretColumn);
        if (leaderIndex < 0)
            return false;

        var nextLeft = text.IndexOf('<', leaderIndex + 1);
        var nextRight = text.IndexOf('>', leaderIndex + 1);
        if ((nextLeft == -1 && nextRight != -1) || nextLeft > nextRight)
            // 如果 光标 后面有 > 且没有 < 或者 < 的位置大于 > 的位置，那么肯定是封好了一个，那么这里就不必补全了
            return false;

        session = new ChatCompletionSession(leaderIndex, caretColumn - leaderIndex,
            text[(leaderIndex + 1)..caretColumn], provider);
        return true;
    }
}
