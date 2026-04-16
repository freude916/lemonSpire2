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

        var endIndex = text.IndexOf('>', startIndex);
        if (endIndex < 0)
            return false;

        var body = text[(startIndex + 1)..endIndex];
        var separatorIndex = body.IndexOf(':', StringComparison.InvariantCulture);
        if (separatorIndex <= 0 || separatorIndex >= body.Length - 1)
            return false;

        if (!inlineReferences.TryGet(body[..separatorIndex], out var referenceType) || referenceType is null)
            return false;

        if (!referenceType.TryResolve(body[(separatorIndex + 1)..], out segment))
            return false;

        length = endIndex - startIndex + 1;
        return true;
    }
}
