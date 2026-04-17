using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Input.Abstractions;

public interface IChatInlineReference
{
    string TypeName { get; }
    IReadOnlyList<ChatCompletionItem> GetCompletions(string query);
    bool TryResolve(string payload, out IMsgSegment segment);
}
