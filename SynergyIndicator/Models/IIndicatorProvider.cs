using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using IndicatorType = lemonSpire2.SynergyIndicator.IndicatorRegistry.IndicatorType;

namespace lemonSpire2.SynergyIndicator.Models;

public interface IIndicatorProvider
{
    IndicatorType Type { get; }
    bool ShouldShow(IEnumerable<CardModel> handCards);


    public static bool CardAppliesPowerToEnemy<T>(CardModel card) where T : PowerModel
    {
        ArgumentNullException.ThrowIfNull(card);
        var key = typeof(T).Name;
        var hasPower = card.DynamicVars.TryGetValue(key, out var dynVar) && dynVar is PowerVar<T>;
        return hasPower && card.TargetType is TargetType.AllEnemies or TargetType.RandomEnemy or TargetType.AnyEnemy;
    }

    public static bool CardAppliesPowerToAllies<T>(CardModel card) where T : PowerModel
    {
        ArgumentNullException.ThrowIfNull(card);
        var key = typeof(T).Name;
        var hasPower = card.DynamicVars.TryGetValue(key, out var dynVar) && dynVar is PowerVar<T>;
        return hasPower && card.TargetType is TargetType.AllAllies or TargetType.AnyAlly;
    }

    public static bool CardHasVarToAllies(CardModel card, string dynamicVarName)
    {
        ArgumentNullException.ThrowIfNull(card);
        var hasVar = card.DynamicVars.TryGetValue(dynamicVarName, out _);
        return hasVar && card.TargetType is TargetType.AllAllies or TargetType.AnyAlly;
    }
}
