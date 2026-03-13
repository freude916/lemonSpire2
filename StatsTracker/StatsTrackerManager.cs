using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.StatsTracker;

public class StatsTrackerManager
{
    private static StatsTrackerManager? _instance;

    private readonly Dictionary<ulong, StatsValues> _playerStats = new();
    private readonly HashSet<int> _processedHashes = new();

    private StatsTrackerManager()
    {
    }

    public static StatsTrackerManager Instance => _instance ??= new StatsTrackerManager();

    public void Initialize()
    {
        RunManager.Instance.RunStarted += OnRunStarted;
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    private void OnRunStarted(RunState _)
    {
        Reset();
    }

    private void OnCombatSetUp(CombatState _)
    {
        foreach (var stats in _playerStats.Values) stats.ResetCombatStats();

        _processedHashes.Clear();
        CombatManager.Instance.History.Changed += OnHistoryChanged;
    }

    private void OnCombatEnded(CombatRoom _)
    {
        // Clear processed hashes
        _processedHashes.Clear();
        CombatManager.Instance.History.Changed -= OnHistoryChanged;
    }

    private void OnHistoryChanged()
    {
        var entries = CombatManager.Instance.History.Entries;
        foreach (var entry in entries)
        {
            var hash = entry.GetHashCode();
            if (!_processedHashes.Add(hash)) continue;

            if (entry is DamageReceivedEntry damageEntry) ProcessDamageEntry(damageEntry);
        }
    }

    private void ProcessDamageEntry(DamageReceivedEntry entry)
    {
        var dealer = entry.Dealer;
        if (dealer == null) return;

        var player = dealer.IsPlayer ? dealer.Player : dealer.PetOwner;
        if (player == null) return;

        var damage = entry.Result.TotalDamage;
        if (damage <= 0) return;

        var extraDamage = 0;
        var cardSource = entry.CardSource;
        if (cardSource != null)
        {
            var vars = cardSource.DynamicVars;
            decimal baseDamage = 0;

            // Try to get base damage from various possible dynamic vars, prioritize CalculatedDamage for accuracy
            if (vars.TryGetValue("CalculatedDamage", out var calcVar) && calcVar is CalculatedVar calculatedVar)
                // `CalculatedDamage.BaseValue` seems to be equal to Damage.BaseValue
                // But CalculatedDamage.Calculate() gives the correct
                baseDamage = calculatedVar.Calculate(null);
            else if (vars.TryGetValue("Damage", out var dmgVar))
                baseDamage = dmgVar.BaseValue;
            else if (vars.TryGetValue("OstyDamage", out var ostyVar)) baseDamage = ostyVar.BaseValue;

            extraDamage = Math.Max(0, damage - (int)baseDamage);
        }

        var stats = GetOrCreateStats(player.NetId);
        stats.Add("stats.combat.damage", damage);
        stats.Add("stats.combat.extra_damage", extraDamage);
        stats.Add("stats.total.damage", damage);
        stats.Add("stats.total.extra_damage", extraDamage);
    }

    public StatsValues GetOrCreateStats(ulong netId)
    {
        if (!_playerStats.TryGetValue(netId, out var stats))
        {
            stats = new StatsValues();
            _playerStats[netId] = stats;
        }

        return stats;
    }

    public StatsValues? GetStats(ulong netId)
    {
        return _playerStats.GetValueOrDefault(netId);
    }

    public void Reset()
    {
        _playerStats.Clear();
        _processedHashes.Clear();
    }
}
