using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;

namespace lemonSpire2.Chat;

public class ChatStore
{
    private readonly INetGameService _netService;

    public ChatStore(INetGameService netService)
    {
        Model = new ChatModel();
        _netService = netService;
        Instance = this;

        _netService.RegisterMessageHandler<ChatMessage>(OnReceiveMessage);

        // 注册核心基础意图的处理逻辑
        IntentRegistry.Register<IntentTextSubmit>(i =>
        {
            // Fill sender ID, name, and UTC timestamp
            var senderId = _netService.NetId;
            var senderName = PlatformUtil.GetPlayerName(_netService.Platform, senderId);
            var msg = new ChatMessage
            {
                SenderId = senderId, // Will be filled by ChatStore
                SenderName = senderName,
                Timestamp = DateTime.Now,
                Segments = [new RichTextSegment { Text = i.Text }]
            };
            Dispatch(new IntentSendMessage { Message = msg });
        });

        IntentRegistry.Register<IntentSendSegments>(i =>
        {
            var senderId = i.senderId ?? _netService.NetId;
            var senderName = PlatformUtil.GetPlayerName(_netService.Platform, senderId);
            var receiverId = i.receiverId ?? 0; // default to broadcast
            var msg = new ChatMessage
            {
                SenderId = senderId,
                SenderName = senderName,
                ReceiverId = receiverId,
                Timestamp = DateTime.Now,
                Segments = i.Segments
            };
            Dispatch(new IntentSendMessage { Message = msg });
        });

        IntentRegistry.Register<IntentSendMessage>(i =>
            OnSendMessage(i.Message)
        );
        IntentRegistry.Register<IntentReceiveMessage>(i => { Model.AppendMessage(i.Message); });
    }

    /// <summary>
    ///     当前活跃的 ChatStore 实例，供其他模块（如 SendItem）使用
    /// </summary>
    public static ChatStore? Instance { get; internal set; }

    public ChatModel Model { get; init; }

    // Expose for external handlers (e.g., TooltipManager)
    public IntentHandlerRegistry IntentRegistry { get; } = new();


    public bool Dispatch(IIntent intent)
    {
        // 先去注册表里找有没有人能处理这个 Intent
        return IntentRegistry.TryHandle(intent);
    }

    private void OnSendMessage(ChatMessage message)
    {
        // 广播
        _netService.SendMessage(message);

        // 发送者回显（STS2 的广播不包含发送者自己）
        OnReceiveMessage(message, message.SenderId);
    }

    private void OnReceiveMessage(ChatMessage chatMessage, ulong senderId)
    {
        MainFile.Logger.Debug($"OnReceiveMessage: senderId={senderId}, msgSenderId={chatMessage.SenderId}");

        ArgumentNullException.ThrowIfNull(chatMessage);

        if (senderId != 0 && chatMessage.SenderId != senderId)
            MainFile.Logger.Warn(
                $"Received chat message with mismatched sender ID! SenderId: {senderId}, Message.SenderId: {chatMessage.SenderId}");

        if (chatMessage.ReceiverId != 0 && chatMessage.ReceiverId != _netService.NetId)
        {
            MainFile.Logger.Debug($"Message not for me: receiverId={chatMessage.ReceiverId}, myId={_netService.NetId}");
            return; // 不是发给我的消息，忽略
        }

        var intentReceiveMessage = new IntentReceiveMessage
        {
            Message = chatMessage
        };

        MainFile.Logger.Debug("Dispatching IntentReceiveMessage");
        if (Dispatch(intentReceiveMessage))
        {
            MainFile.Logger.Debug("IntentReceiveMessage dispatched successfully");
            return;
        }

        MainFile.Logger.Error("Basic intent registered, should not happen! ");
    }
}
