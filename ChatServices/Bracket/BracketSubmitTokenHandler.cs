using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Registry;
using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Input.Service.Bracket;

public sealed class BracketSubmitTokenHandler(ChatInlineReferenceRegistry inlineReferences) : IChatSubmitTokenHandler
{
    public char TriggerChar => '<';

    public bool TryParse(string text, int startIndex, out IMsgSegment segment, out int length)
    {
        ArgumentNullException.ThrowIfNull(text);
        segment = null!;
        length = 0;

        var closingChar = '>';
        var endIndex = text.IndexOf(closingChar, startIndex);
        if (endIndex < 0)
            return false;

        var body = text[(startIndex + 1)..endIndex];
        var separatorIndex = body.IndexOf(':', StringComparison.InvariantCulture);
        if (separatorIndex <= 0 || separatorIndex >= body.Length - 1)
            return false;

        // 这里 submit 阶段只做“type + payload”的分发，真正把 payload 解释成 segment 的责任留给具体类型。
        if (!inlineReferences.TryGet(body[..separatorIndex], out var Reference) || Reference is null)
            return false;

        if (!Reference.TryResolve(body[(separatorIndex + 1)..], out segment))
            return false;

        length = endIndex - startIndex + 1;
        return true;
    }
}
