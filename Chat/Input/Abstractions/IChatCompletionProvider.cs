using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Abstractions;

public interface IChatCompletionProvider
{
    IReadOnlyList<ChatCompletionItem> GetItems(string query);
}
