using lemonSpire2.Chat.Input;
using lemonSpire2.Chat.Input.Command;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;

namespace lemonSpire2.Chat;

public class ChatStore
{
    private readonly INetGameService _netService;
    private Action<string>? _inputTextInserter;

    public ChatStore(INetGameService netService)
    {
        Model = new ChatModel();
        _netService = netService;
        Instance = this;

        _netService.RegisterMessageHandler<ChatMessage>(OnReceiveMessage);

        // 注册核心基础意图的处理逻辑
        IntentRegistry.Register<IntentTextSubmit>(i =>
        {
            var text = BbCodeUtils.AutoCloseUnclosedTags(i.Text);

            // Slash 命令在本地先消费；只有明确判定“不是命令”时，才继续走普通聊天解析。
            var commandResult = InputServices.CommandProcessor.Process(text);
            if (commandResult is not NotAChatCmdResult)
            {
                HandleCommandResult(commandResult);
                return true;
            }

            var result = InputServices.Parser.Parse(text);

            Dispatch(new IntentSendSegments
            {
                Segments = result
            });

            return true;
        });

        IntentRegistry.Register<IntentSendSegments>(i =>
        {
            var senderId = i.SenderId ?? _netService.NetId;
            var senderName = PlatformUtil.GetPlayerName(_netService.Platform, senderId);
            var receiverId = i.ReceiverId ?? 0; // default to broadcast
            var msg = new ChatMessage
            {
                SenderId = senderId,
                SenderName = senderName,
                ReceiverId = receiverId,
                Timestamp = DateTimeOffset.UtcNow,
                Segments = i.Segments
            };
            Dispatch(new IntentSendMessage { Message = msg });
            return true;
        });

        IntentRegistry.Register<IntentSendMessage>(i =>
            {
                OnSendMessage(i.Message);
                return true;
            }
        );
        IntentRegistry.Register<IntentReceiveMessage>(i =>
        {
            Model.AppendMessage(i.Message);
            return true;
        });
    }

    private static Logger Log => ChatUiPatch.Log;

    /// <summary>
    ///     当前活跃的 ChatStore 实例，供其他模块（如 SendItem）使用
    /// </summary>
    public static ChatStore? Instance { get; internal set; }

    public ChatModel Model { get; init; }
    public ChatInputServices InputServices { get; } = new();

    public ulong LocalNetId => _netService.NetId;

    // Expose for external handlers (e.g., TooltipManager)
    public IntentHandlerRegistry IntentRegistry { get; } = new();


    public bool Dispatch(IIntent intent)
    {
        // 先去注册表里找有没有人能处理这个 Intent
        return IntentRegistry.TryHandle(intent);
    }

    public void RegisterInputTextInserter(Action<string> inserter)
    {
        ArgumentNullException.ThrowIfNull(inserter);
        _inputTextInserter = inserter;
    }

    private void HandleCommandResult(ChatCmdResult result)
    {
        switch (result)
        {
            // 纯本地反馈：例如 /help、参数错误等，直接回灌到聊天模型，不发网。
            case LocalDisplayChatCmdResult display:
                AppendLocalSystemMessage(display.HeaderText, display.Text);
                return;
            case ErrorChatCmdResult error:
                AppendLocalSystemMessage(error.HeaderText, error.Message);
                return;
            // 命令执行产出了“像普通消息一样发送的 segment”，这里复用现有发送链路。
            case SendSegmentsChatCmdResult send:
                foreach (var message in send.Messages)
                    Dispatch(new IntentSendSegments
                    {
                        ReceiverId = message.ReceiverId,
                        Segments = message.Segments
                    });
                return;
            case NotAChatCmdResult:
                return;
            default:
                throw new InvalidOperationException($"Unknown chat command result type: {result.GetType().FullName}");
        }
    }

    private void AppendLocalSystemMessage(string headerText, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerText);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // 这里故意走 IntentReceiveMessage 而不是 SendMessage：
        // 系统消息只显示给本地，不进入网络层，也不触发发送者回显逻辑。
        Dispatch(new IntentReceiveMessage
        {
            Message = new ChatMessage
            {
                SenderId = 0,
                SenderName = headerText,
                ReceiverId = LocalNetId,
                Timestamp = DateTimeOffset.UtcNow,
                Segments =
                [
                    new TextDisplaySegment
                    {
                        HeaderText = headerText,
                        Text = text
                    }
                ]
            }
        });
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
        ChatUiPatch.Log.Debug($"OnReceiveMessage: senderId={senderId}, msgSenderId={chatMessage.SenderId}");

        ArgumentNullException.ThrowIfNull(chatMessage);

        if (senderId != 0 && chatMessage.SenderId != senderId)
            ChatUiPatch.Log.Warn(
                $"Received chat message with mismatched sender ID! SenderId: {senderId}, Message.SenderId: {chatMessage.SenderId}");

        if (chatMessage.ReceiverId != 0 && chatMessage.ReceiverId != _netService.NetId)
        {
            ChatUiPatch.Log.Debug($"Message not for me: receiverId={chatMessage.ReceiverId}, myId={_netService.NetId}");
            return; // 不是发给我的消息，忽略
        }

        chatMessage.NotificationSound = ResolveNotificationSound(chatMessage);

        var intentReceiveMessage = new IntentReceiveMessage
        {
            Message = chatMessage
        };

        if (Dispatch(intentReceiveMessage))
        {
            ChatUiPatch.Log.Debug("IntentReceiveMessage dispatched.");
            return;
        }

        ChatUiPatch.Log.Error("Basic intent registered, should not happen! ");
    }

    private ChatNotificationSound ResolveNotificationSound(ChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);

        if (chatMessage.SenderId != _netService.NetId && ContainsMentionForLocalPlayer(chatMessage))
            return ChatNotificationSound.AtMessage;

        return ChatNotificationSound.ReceiveMessage;
    }

    private bool ContainsMentionForLocalPlayer(ChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);

        return chatMessage.Segments.OfType<EntitySegment>()
            .Any(segment =>
                segment.Kind == EntitySegment.EntityKind.Player && segment.PlayerNetId == _netService.NetId);
    }

    /// <summary>
    ///     发送消息片段到聊天
    /// </summary>
    public static void SendToChat(params IMsgSegment[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var store = Instance;
        if (store == null)
        {
            Log.Warn("ChatStore.Instance is null");
            return;
        }

        store.Dispatch(new IntentSendSegments
        {
            ReceiverId = 0,
            Segments = segments
        });
        Log.Info($"Sent to chat: {string.Join(", ", segments.Select(s => s.Render()))}");
    }

    public static bool TryInsertIntoInput(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var store = Instance;
        if (store == null)
        {
            Log.Warn("ChatStore.Instance is null");
            return false;
        }

        if (store._inputTextInserter == null)
        {
            Log.Warn("Chat input inserter is not registered");
            return false;
        }

        store._inputTextInserter(text);
        return true;
    }
}
