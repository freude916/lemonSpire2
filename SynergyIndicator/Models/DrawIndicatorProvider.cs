using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using IndicatorType = lemonSpire2.SynergyIndicator.IndicatorRegistry.IndicatorType;

namespace lemonSpire2.SynergyIndicator.Models;

public class DrawIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Draw;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(card => IIndicatorProvider.CardHasVarToAllies(card, nameof(DynamicVarSet.Cards)));
    }
}
