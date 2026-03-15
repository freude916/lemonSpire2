using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌奖励同步静态入口
/// </summary>
public static class CardRewardSynchronizer
{
    private static CardRewardNetworkHandler? _handler;

    public static void Initialize(INetGameService netService)
    {
        _handler?.Dispose();
        _handler = new CardRewardNetworkHandler(netService);
    }

    public static void BroadcastCardReward(CardRewardGroup group)
    {
        _handler?.BroadcastCardReward(group);
    }

    public static void BroadcastClearRewards()
    {
        _handler?.BroadcastClearRewards();
    }

    public static void Dispose()
    {
        _handler?.Dispose();
        _handler = null;
    }
}
