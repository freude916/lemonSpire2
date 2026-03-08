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
        foreach (var stats in _playerStats.Values)
        {
            stats.ResetCombatStats();
        }

        _processedHashes.Clear();
        CombatManager.Instance.History.Changed += OnHistoryChanged;
    }

    private void OnCombatEnded(CombatRoom _)
    {
        // 只清除已处理的历史记录哈希，保留玩家统计数据供 Tooltip 显示
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
        
        var damage = entry.Result.TotalDamage;
        if (damage <= 0)
        {
            return;
        }

        var extraDamage = 0;
        var cardSource = entry.CardSource;
        if (cardSource != null)
        {
            var vars = cardSource.DynamicVars;
            decimal baseDamage = 0;

            // 尝试获取三种伤害变量（按优先级）
            if (vars.TryGetValue("CalculatedDamage", out var calcVar) && calcVar is CalculatedVar calculatedVar)
            {
                // CalculatedDamage 的 BaseValue 只是 CalculationBase，需要用 Calculate() 获取完整卡牌伤害
                baseDamage = calculatedVar.Calculate(null);
            }
            else if (vars.TryGetValue("Damage", out var dmgVar))
            {
                baseDamage = dmgVar.BaseValue;
            }
            else if (vars.TryGetValue("OstyDamage", out var ostyVar))
            {
                baseDamage = ostyVar.BaseValue;
            }

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

    public StatsValues? GetStats(ulong netId) => _playerStats.GetValueOrDefault(netId);

    public void Reset()
    {
        _playerStats.Clear();
        _processedHashes.Clear();
    }
}