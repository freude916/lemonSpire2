using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Rooms;

namespace lemonSpire2.DamageTracker;

public class DamageTrackerManager
{
    private static DamageTrackerManager? _instance;
    public static DamageTrackerManager Instance => _instance ??= new DamageTrackerManager();

    private readonly Dictionary<ulong, int> _combatDamage = new();
    private readonly Dictionary<ulong, long> _totalDamage = new();
    private readonly HashSet<int> _processedEntryHashes = new();

    public IReadOnlyDictionary<ulong, int> CombatDamage => _combatDamage;
    public IReadOnlyDictionary<ulong, long> TotalDamage => _totalDamage;

    private DamageTrackerManager()
    {
    }

    public void Initialize()
    {
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    private void OnCombatSetUp(CombatState _)
    {
        _combatDamage.Clear();
        _processedEntryHashes.Clear();
        CombatManager.Instance.History.Changed += OnHistoryChanged;
    }

    private void OnCombatEnded(CombatRoom _)
    {
        _combatDamage.Clear();
        _processedEntryHashes.Clear();
        CombatManager.Instance.History.Changed -= OnHistoryChanged;
    }

    private void OnHistoryChanged()
    {
        var entries = CombatManager.Instance.History.Entries;
        foreach (var entry in entries)
        {
            if (entry is DamageReceivedEntry damageEntry)
            {
                // Use hash to avoid processing the same entry multiple times
                int hash = entry.GetHashCode();
                if (_processedEntryHashes.Add(hash))
                {
                    ProcessDamageEntry(damageEntry);
                }
            }
        }
    }

    private void ProcessDamageEntry(DamageReceivedEntry entry)
    {
        var dealer = entry.Dealer;
        if (dealer == null) return;

        // Get player - either directly or through pet owner (e.g., Osteo's Osty)
        var player = dealer.IsPlayer ? dealer.Player : dealer.PetOwner;
        if (player == null) return;

        var damage = entry.Result.UnblockedDamage;
        if (damage <= 0) return;

        ulong netId = player.NetId;

        // Update both damage trackers
        _combatDamage.TryGetValue(netId, out int combat);
        _totalDamage.TryGetValue(netId, out long total);

        _combatDamage[netId] = combat + damage;
        _totalDamage[netId] = total + damage;
    }

    public int GetCombatDamage(ulong netId)
    {
        return _combatDamage.GetValueOrDefault(netId, 0);
    }

    public long GetTotalDamage(ulong netId)
    {
        return _totalDamage.GetValueOrDefault(netId, 0);
    }

    public void Reset()
    {
        _combatDamage.Clear();
        _totalDamage.Clear();
        _processedEntryHashes.Clear();
    }
}
