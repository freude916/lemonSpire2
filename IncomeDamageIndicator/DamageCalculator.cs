using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace lemonSpire2.IncomeDamageIndicator;

/// <summary>
/// 敌人攻击信息
/// </summary>
public class EnemyAttackInfo
{
    public string EnemyName { get; set; } = "";
    public int TotalDamage { get; set; }
    public int SingleDamage { get; set; }
    public int Repeats { get; set; }
}

/// <summary>
/// 伤害计算工具类
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 计算所有敌人对本玩家的总伤害（考虑多段攻击）
    /// </summary>
    public static int CalculateTotalIncomingDamage(Player player)
    {
        if (player.Creature.CombatState == null)
            return 0;

        CombatState combatState = player.Creature.CombatState;
        Creature playerCreature = player.Creature;
        int totalDamage = 0;

        // 获取所有存活的敌人
        foreach (Creature enemy in combatState.Enemies.Where(e => e.IsAlive))
        {
            MonsterModel? monster = enemy.Monster;
            if (monster == null)
                continue;

            // 获取敌人的下一个行动意图
            IReadOnlyList<AbstractIntent> intents = monster.NextMove.Intents;
            
            foreach (AbstractIntent intent in intents)
            {
                if (intent is AttackIntent attackIntent)
                {
                    // 检查目标是否包含当前玩家
                    var targets = combatState.PlayerCreatures;
                    bool targetsPlayer = targets.Contains(playerCreature);
                    
                    if (targetsPlayer)
                    {
                        // 获取总伤害（包括多段攻击）
                        int damage = attackIntent.GetTotalDamage(targets, enemy);
                        totalDamage += damage;
                    }
                }
            }
        }

        return totalDamage;
    }

    /// <summary>
    /// 获取所有敌人的攻击意图详情
    /// </summary>
    public static List<EnemyAttackInfo> GetEnemyAttackInfos(Player player)
    {
        var result = new List<EnemyAttackInfo>();
        
        if (player.Creature.CombatState == null)
            return result;

        CombatState combatState = player.Creature.CombatState;
        var targets = combatState.PlayerCreatures.ToList();

        foreach (Creature enemy in combatState.Enemies.Where(e => e.IsAlive))
        {
            MonsterModel? monster = enemy.Monster;
            if (monster == null)
                continue;

            IReadOnlyList<AbstractIntent> intents = monster.NextMove.Intents;
            
            foreach (AbstractIntent intent in intents)
            {
                if (intent is AttackIntent attackIntent)
                {
                    int damage = attackIntent.GetTotalDamage(targets, enemy);
                    int singleDamage = attackIntent.GetSingleDamage(targets, enemy);
                    int repeats = attackIntent.Repeats;
                    
                    result.Add(new EnemyAttackInfo
                    {
                        EnemyName = enemy.Name,
                        TotalDamage = damage,
                        SingleDamage = singleDamage,
                        Repeats = repeats
                    });
                }
            }
        }

        return result;
    }
}
