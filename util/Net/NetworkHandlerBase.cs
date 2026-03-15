using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.util.Net;

/// <summary>
///     网络消息处理器基类
///     提供统一的消息注册、发送、过滤逻辑
/// </summary>
/// <typeparam name="TMessage">消息类型</typeparam>
public abstract class NetworkHandlerBase<TMessage> : IDisposable where TMessage : INetMessage
{
    private readonly INetGameService _netService;
    private bool _disposed;

    protected NetworkHandlerBase(INetGameService netService)
    {
        _netService = netService ?? throw new ArgumentNullException(nameof(netService));
        _netService.RegisterMessageHandler<TMessage>(OnReceiveMessage);
    }

    /// <summary>
    ///     本地玩家 NetId
    /// </summary>
    protected ulong LocalPlayerId => _netService.NetId;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _netService.UnregisterMessageHandler<TMessage>(OnReceiveMessage);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     检测是否为自己发送的消息
    /// </summary>
    protected bool IsSelf(ulong senderId)
    {
        return senderId == LocalPlayerId;
    }

    /// <summary>
    ///     发送消息
    /// </summary>
    protected void SendMessage(TMessage message)
    {
        _netService.SendMessage(message);
    }

    /// <summary>
    ///     接收消息处理（子类实现）
    /// </summary>
    protected abstract void OnReceiveMessage(TMessage message, ulong senderId);
}
