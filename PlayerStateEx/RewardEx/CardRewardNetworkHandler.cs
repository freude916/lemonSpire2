using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace lemonSpire2.PlayerStateEx.RewardEx;

/// <summary>
///     卡牌奖励网络同步处理器
///     负责广播本地玩家的卡牌奖励选择，接收其他玩家的卡牌奖励数据
/// </summary>
public sealed class CardRewardNetworkHandler : NetworkHandlerBase<CardRewardMessage>
{
    public CardRewardNetworkHandler(INetGameService netService) : base(netService)
    {
        MainFile.Logger.Info("[CardRewardNetworkHandler] Initialized");
    }

    /// <summary>
    ///     广播卡牌奖励组
    /// </summary>
    public void BroadcastCardReward(CardRewardGroup group)
    {
        var message = new CardRewardMessage
        {
            SenderId = LocalPlayerId,
            Group = group,
            IsClear = false
        };

        SendMessage(message);
        // Host 广播时自己收不到，手动处理
        OnReceiveMessage(message, LocalPlayerId);
        MainFile.Logger.Info($"[CardRewardNetworkHandler] Broadcasted card reward group {group.GroupId} with {group.Cards.Count} cards");
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
        MainFile.Logger.Debug("[CardRewardNetworkHandler] Broadcasted clear message");
    }

    protected override void OnReceiveMessage(CardRewardMessage message, ulong senderId)
    {
        MainFile.Logger.Debug(
            $"[CardRewardNetworkHandler] Received card reward from player {message.SenderId}, isClear={message.IsClear}");

        if (message.IsClear)
        {
            CardRewardManager.Instance.ClearGroups(message.SenderId);
        }
        else
        {
            CardRewardManager.Instance.AddGroup(message.SenderId, message.Group);
        }
    }
}
