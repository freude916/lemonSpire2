using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;

namespace lemonSpire2.Chat;

/// <summary>
///     聊天管理器 - 负责消息收发和网络同步
/// </summary>
public class ChatManager : IDisposable
{
    private const int MaxHistorySize = 100;
    private static ChatManager? _instance;

    private readonly List<ChatMessageEntry> _messageHistory = new();
    private ChatUi? _chatUI;
    private bool _isInitialized;

    private INetGameService? _netService;

    private ChatManager()
    {
    }

    public static ChatManager Instance => _instance ??= new ChatManager();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event Action<ChatMessageEntry>? OnMessageReceived;

    public void Initialize(INetGameService netService)
    {
        if (_isInitialized)
        {
            Cleanup();
        }

        _netService = netService;
        _netService.RegisterMessageHandler<ChatMessage>(OnChatMessageReceived);
        _isInitialized = true;

        MainFile.Logger.Info("ChatManager initialized");
    }

    public void SetChatUI(ChatUi chatUI)
    {
        _chatUI = chatUI;
        _chatUI.OnMessageSent += OnLocalMessageSent;

        // 更新可见性
        UpdateVisibility();
    }

    private void OnChatMessageReceived(ChatMessage message, ulong senderId)
    {
        // 忽略自己发的消息（已在 OnLocalMessageSent 中显示）
        if (_netService != null && message.senderId == _netService.NetId)
        {
            return;
        }

        var entry = new ChatMessageEntry(
            message.senderId,
            message.senderName,
            message.content,
            message.timestamp
        );

        AddToHistory(entry);
        OnMessageReceived?.Invoke(entry);

        // 在主线程更新UI
        if (_chatUI != null && GodotObject.IsInstanceValid(_chatUI))
        {
            _chatUI.CallDeferred(nameof(ChatUi.AddMessage),
                message.senderId, message.senderName, message.content, message.timestamp);
        }

        MainFile.Logger.Debug($"Chat message received from {message.senderName}: {message.content}");
    }

    private void OnLocalMessageSent(string content)
    {
        if (_netService == null || !_netService.IsConnected)
        {
            MainFile.Logger.Warn("Cannot send chat message: not connected");
            return;
        }

        // 获取当前玩家名称
        var playerName = GetLocalPlayerName();

        var message = new ChatMessage
        {
            senderId = _netService.NetId,
            senderName = playerName,
            content = content,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // 发送到网络
        _netService.SendMessage(message);

        // 立即显示自己发的消息
        // 注意：虽然 ShouldBroadcast = true，但 Host 作为服务器不会收到自己广播的消息
        // 所以需要在这里直接显示，而 Client 会通过广播回调收到（避免重复）
        var entry = new ChatMessageEntry(message.senderId, message.senderName, message.content, message.timestamp);
        AddToHistory(entry);

        // 直接在 UI 显示本地消息
        if (_chatUI != null && GodotObject.IsInstanceValid(_chatUI))
        {
            _chatUI.CallDeferred(nameof(ChatUi.AddMessage),
                message.senderId, message.senderName, message.content, message.timestamp);
        }

        MainFile.Logger.Debug($"Chat message sent: {content}");
    }

    private string GetLocalPlayerName()
    {
        if (_netService == null)
        {
            return "Player";
        }

        return PlatformUtil.GetPlayerName(_netService.Platform, _netService.NetId) ?? "Player";
    }

    private void AddToHistory(ChatMessageEntry entry)
    {
        _messageHistory.Add(entry);

        while (_messageHistory.Count > MaxHistorySize)
        {
            _messageHistory.RemoveAt(0);
        }
    }

    public void UpdateVisibility()
    {
        if (_chatUI != null && GodotObject.IsInstanceValid(_chatUI))
        {
            var isMultiplayer = _netService != null && _netService.Type.IsMultiplayer();
            _chatUI.CallDeferred(nameof(ChatUi.SetVisibleForMultiplayer), isMultiplayer);
        }
    }

    public void Cleanup()
    {
        if (_netService != null)
        {
            _netService.UnregisterMessageHandler<ChatMessage>(OnChatMessageReceived);
        }

        if (_chatUI != null)
        {
            _chatUI.OnMessageSent -= OnLocalMessageSent;
        }

        _messageHistory.Clear();
        _isInitialized = false;

        MainFile.Logger.Info("ChatManager cleaned up");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Cleanup();
        _instance = null;
    }
}