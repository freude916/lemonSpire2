using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌奖励管理器
///     存储所有玩家的卡牌奖励组
/// </summary>
public class CardRewardManager
{
    /// <summary>
    ///     每个玩家最多保留的奖励组数量
    /// </summary>
    private const int MaxGroupsPerPlayer = 5;

    /// <summary>
    ///     玩家 NetId -> 奖励组列表
    /// </summary>
    private readonly ConcurrentDictionary<ulong, Collection<CardRewardGroup>> _playerRewards = new();

    private CardRewardManager()
    {
    }

    private static Logger Log => CardRewardNetworkHandler.Log;

    public static CardRewardManager Instance { get; } = new();

    /// <summary>
    ///     奖励更新事件，参数为更新的玩家 NetId
    /// </summary>
    public event Action<ulong>? RewardsUpdated;

    /// <summary>
    ///     添加奖励组
    /// </summary>
    public void AddGroup(ulong playerNetId, CardRewardGroup group)
    {
        var groups = _playerRewards.GetOrAdd(playerNetId, _ => []);
        groups.Add(group);

        // 保持最多 MaxGroupsPerPlayer 个组
        while (groups.Count > MaxGroupsPerPlayer) groups.RemoveAt(0);

        Log.Debug($"AddGroup: player={playerNetId}, groupId={group.GroupId}, cards={group.Cards.Count}");
        RewardsUpdated?.Invoke(playerNetId);
    }

    /// <summary>
    ///     清除玩家的所有奖励组
    /// </summary>
    public void ClearGroups(ulong playerNetId)
    {
        _playerRewards.TryRemove(playerNetId, out _);
        Log.Debug($"ClearGroups: player={playerNetId}");
        RewardsUpdated?.Invoke(playerNetId);
    }

    /// <summary>
    ///     获取玩家的奖励组（排除被盗牌归还）
    /// </summary>
    public Collection<CardRewardGroup> GetGroups(ulong playerNetId)
    {
        if (!_playerRewards.TryGetValue(playerNetId, out var groups)) return [];
        return groups;
    }

    /// <summary>
    ///     检查玩家是否有奖励数据
    /// </summary>
    public bool HasRewards(ulong playerNetId)
    {
        return GetGroups(playerNetId).Count > 0;
    }

    /// <summary>
    ///     重置数据（只清除数据，不创建新实例，保留事件订阅者）
    /// </summary>
    public static void Reset()
    {
        Instance._playerRewards.Clear();
        Log.Debug("Reset: cleared all rewards");
    }
}
