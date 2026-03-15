using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.StatsTracker;

public class StatsTrackerManager
{
    private static StatsTrackerManager? _instance;

    private readonly Dictionary<ulong, StatsValues> _playerStats = new();
    private readonly HashSet<int> _processedHashes = new();

    private StatsTrackerManager()
    {
    }

    internal static Logger Log { get; } = new("lemon.stats", LogType.Actions);

    public static StatsTrackerManager Instance => _instance ??= new StatsTrackerManager();

    public void Initialize()
    {
        RunManager.Instance.RunStarted += OnRunStarted;
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        Log.Info("StatsTrackerManager initialized");
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

            switch (entry)
            {
                case DamageReceivedEntry damageEntry:
                    ProcessDamageEntry(damageEntry);
                    break;
                case PowerReceivedEntry powerEntry:
                    ProcessPowerEntry(powerEntry);
                    break;
            }
        }
    }

    private void ProcessPowerEntry(PowerReceivedEntry entry)
    {
        var applier = entry.Applier;
        if (applier == null)
        {
            Log.Debug("ProcessPowerEntry: Applier is null");
            return;
        }

        var player = applier.IsPlayer ? applier.Player : applier.PetOwner;
        if (player == null)
        {
            Log.Debug("ProcessPowerEntry: player is null");
            return;
        }

        var power = entry.Power;
        var amount = (int)entry.Amount;
        if (amount <= 0) return;

        // 使用 entry.Actor 代替 power.Owner，避免 AssertMutable() 问题
        // Actor 是在创建 PowerReceivedEntry 时从 power.Owner 传入的
        var target = entry.Actor;
        if (target == null)
        {
            Log.Debug("ProcessPowerEntry: target (Actor) is null");
            return;
        }

        var isTargetPlayer = target.IsPlayer || target.PetOwner != null;

        var stats = GetOrCreateStats(player.NetId);

        // 施加者和目标是同一人 → 自身能力，暂不统计
        if (applier == target) return;

        if (isTargetPlayer)
        {
            // 给队友上 buff
            if (power.Type == PowerType.Buff)
            {
                Log.Debug($"ProcessPowerEntry: buff {amount} from player {player.NetId}");
                stats.Add("stats.combat.buffs_applied", amount);
                stats.Add("stats.total.buffs_applied", amount);
            }
        }
        else
        {
            // 给敌人上 debuff
            if (power.Type == PowerType.Debuff)
            {
                Log.Debug($"ProcessPowerEntry: debuff {amount} from player {player.NetId}");
                stats.Add("stats.combat.debuffs_applied", amount);
                stats.Add("stats.total.debuffs_applied", amount);
            }
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
