using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌奖励网络同步处理器
///     负责广播本地玩家的卡牌奖励选择，接收其他玩家的卡牌奖励数据
/// </summary>
public sealed class CardRewardNetworkHandler : NetworkHandlerBase<CardRewardMessage>
{
    public CardRewardNetworkHandler(INetGameService netService) : base(netService)
    {
        Log.Info("CardRewardNetworkHandler initialized");
    }

    internal static Logger Log { get; } = new("lemon.reward", LogType.GameSync);

    /// <summary>
    ///     广播卡牌奖励组
    /// </summary>
    public void BroadcastCardReward(CardRewardGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var message = new CardRewardMessage
        {
            SenderId = LocalPlayerId,
            Group = group,
            IsClear = false
        };

        SendMessage(message);
        // Host 广播时自己收不到，手动处理
        OnReceiveMessage(message, LocalPlayerId);
        Log.Info($"Broadcasted card reward group {group.GroupId} with {group.Cards.Count} cards");
    }

    /// <summary>
    ///     广播清空消息
    /// </summary>
    public void BroadcastClearRewards()
    {
        var message = new CardRewardMessage
        {
            SenderId = LocalPlayerId,
            Group = new CardRewardGroup(),
            IsClear = true
        };

        SendMessage(message);
        // Host 广播时自己收不到，手动处理
        OnReceiveMessage(message, LocalPlayerId);
        Log.Debug("Broadcasted clear message");
    }

    protected override void OnReceiveMessage(CardRewardMessage message, ulong senderId)
    {
        Log.Debug($"Received card reward from player {message.SenderId}, isClear={message.IsClear}");

        if (message.IsClear)
            CardRewardManager.Instance.ClearGroups(message.SenderId);
        else
            CardRewardManager.Instance.AddGroup(message.SenderId, message.Group);
    }
}
