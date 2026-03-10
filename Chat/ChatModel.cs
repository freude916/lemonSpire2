using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat;

public class ChatModel
{
    private List<ChatMessage> Messages { get; } = [];

    public event EventHandler<ChatMessage>? OnMessageAppended;

    public void AppendMessage(ChatMessage message)
    {
        MainFile.Logger.Debug($"ChatModel.AppendMessage: segments={message.Segments.Count}");
        Messages.Add(message);
        OnMessageAppended?.Invoke(this, message);
    }
}