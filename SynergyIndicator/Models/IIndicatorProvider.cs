using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

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
