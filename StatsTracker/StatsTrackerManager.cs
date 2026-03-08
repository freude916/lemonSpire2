using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Rooms;

namespace lemonSpire2.StatsTracker;

public class StatsTrackerManager
{
    private static StatsTrackerManager? _instance;
    public static StatsTrackerManager Instance => _instance ??= new StatsTrackerManager();

    private readonly Dictionary<ulong, StatsValues> _playerStats = new();
    private readonly HashSet<int> _processedHashes = new();

    private StatsTrackerManager()
    {
    }

    public void Initialize()
    {
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    private void OnCombatSetUp(CombatState _)
    {
        foreach (var stats in _playerStats.Values)
        {
            stats.Reset();
        }

        _processedHashes.Clear();
        CombatManager.Instance.History.Changed += OnHistoryChanged;
    }

    private void OnCombatEnded(CombatRoom _)
    {
        foreach (var stats in _playerStats.Values)
        {
            stats.Reset();
        }

        _processedHashes.Clear();
        CombatManager.Instance.History.Changed -= OnHistoryChanged;
    }

    private void OnHistoryChanged()
    {
        var entries = CombatManager.Instance.History.Entries;
        foreach (var entry in entries)
        {
            var hash = entry.GetHashCode();
            if (!_processedHashes.Add(hash))
            {
                continue;
            }

            if (entry is DamageReceivedEntry damageEntry)
            {
                ProcessDamageEntry(damageEntry);
            }
        }
    }

    private void ProcessDamageEntry(DamageReceivedEntry entry)
    {
        var dealer = entry.Dealer;
        if (dealer == null)
        {
            return;
        }

        var player = dealer.IsPlayer ? dealer.Player : dealer.PetOwner;
        if (player == null)
        {
            return;
        }

        var damage = entry.Result.UnblockedDamage;
        if (damage <= 0)
        {
            return;
        }

        var stats = GetOrCreateStats(player.NetId);
        stats.Add("stats.combat_damage", damage);
        stats.Add("stats.total_damage", damage);
    }

    private StatsValues GetOrCreateStats(ulong netId)
    {
        if (!_playerStats.TryGetValue(netId, out var stats))
        {
            stats = new StatsValues();
            _playerStats[netId] = stats;
        }

        return stats;
    }

    public StatsValues? GetStats(ulong netId) => _playerStats.GetValueOrDefault(netId);

    public void Reset()
    {
        _playerStats.Clear();
        _processedHashes.Clear();
    }
}