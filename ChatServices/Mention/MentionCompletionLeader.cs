using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Service.Mention;

public sealed class MentionCompletionLeader(IChatCompletionProvider provider) : IChatCompletionLeader
{
    public char LeaderChar => '@';

    public bool TryMatch(string text, int caretColumn, out ChatCompletionSession session)
    {
        ArgumentNullException.ThrowIfNull(text);
        session = null!;
        var leaderIndex = text.LastIndexOf('@', caretColumn - 1, caretColumn);
        switch (leaderIndex)
        {
            case < 0:
            case > 0 when !char.IsWhiteSpace(text[leaderIndex - 1]):
                return false;
        }

        var query = text[(leaderIndex + 1)..caretColumn];
        if (query.Contains(' '))
            return false;

        session = new ChatCompletionSession(leaderIndex, caretColumn - leaderIndex, query, provider);
        return true;
    }
}
