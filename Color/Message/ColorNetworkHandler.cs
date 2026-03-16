using Godot;
using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace lemonSpire2.PlayerColor;

/// <summary>
///     玩家颜色网络处理器
/// </summary>
public class ColorNetworkHandler : NetworkHandlerBase<PlayerColorMessage>
{
    public ColorNetworkHandler(INetGameService netService) : base(netService)
    {
        ColorManager.Log.Debug("ColorNetworkHandler initialized");
    }

    /// <summary>
    ///     广播玩家颜色变更
    /// </summary>
    public void BroadcastColorChange(ulong playerId, Color color)
    {
        var message = new PlayerColorMessage
        {
            SenderId = LocalPlayerId,
            TargetPlayerId = playerId,
            R = color.R,
            G = color.G,
            B = color.B
        };
        SendMessage(message);
        ColorManager.Log.Debug($"Broadcast color change for player {playerId}: {color}");
    }

    protected override void OnReceiveMessage(PlayerColorMessage message, ulong senderId)
    {
        // 如果是自己的消息，忽略（已经在本地处理过了）
        if (IsSelf(senderId)) return;

        var targetPlayerId = message.TargetPlayerId;
        if (targetPlayerId == 0) targetPlayerId = senderId;

        var color = new Color(message.R, message.G, message.B);
        ColorManager.Instance.ApplyRemoteColor(targetPlayerId, color);
    }
}
