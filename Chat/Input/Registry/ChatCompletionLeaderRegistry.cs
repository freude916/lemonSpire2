using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Registry;

public sealed class ChatCompletionLeaderRegistry
{
    private readonly Dictionary<char, IChatCompletionLeader> _leaders = new();

    public void Register(IChatCompletionLeader leader)
    {
        ArgumentNullException.ThrowIfNull(leader);
        _leaders[leader.LeaderChar] = leader;
    }

    public bool TryMatch(string text, int caretColumn, out ChatCompletionSession session)
    {
        session = null!;
        if (string.IsNullOrEmpty(text) || caretColumn <= 0 || caretColumn > text.Length)
            return false;

        foreach (var leader in _leaders.Values)
            if (leader.TryMatch(text, caretColumn, out session))
                return true;

        return false;
    }
}
