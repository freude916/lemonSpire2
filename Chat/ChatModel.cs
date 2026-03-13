using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat;

public class ChatModel
{
    private List<ChatMessage> Messages { get; } = [];

    public event Action<ChatMessage>? OnMessageAppended;

    public void AppendMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        MainFile.Logger.Debug($"ChatModel.AppendMessage: segments={message.Segments.Count}");
        Messages.Add(message);
        OnMessageAppended?.Invoke(message);
    }
}
