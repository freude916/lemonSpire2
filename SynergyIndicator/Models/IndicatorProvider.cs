using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace lemonSpire2.SynergyIndicator.Models;

public interface IIndicatorProvider
{
    IndicatorType Type { get; }
    bool ShouldShow(IEnumerable<CardModel> handCards);


    public static bool CardAppliesPower<T>(CardModel card) where T : PowerModel
    {
        ArgumentNullException.ThrowIfNull(card);
        var key = typeof(T).Name;
        return card.DynamicVars.TryGetValue(key, out var dynVar) && dynVar is PowerVar<T>;
    }
}

public class HandShakeIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.HandShake;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(c => c.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly);
    }
}

public class VulnerableIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Vulnerable;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(IIndicatorProvider.CardAppliesPower<VulnerablePower>);
    }
}

public class WeakIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Weak;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(IIndicatorProvider.CardAppliesPower<WeakPower>);
    }
}

public class StrangleIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Strangle;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(IIndicatorProvider.CardAppliesPower<StranglePower>);
    }
}
