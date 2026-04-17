using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using IndicatorType = lemonSpire2.SynergyIndicator.IndicatorRegistry.IndicatorType;

namespace lemonSpire2.SynergyIndicator.Models;

public class FlankingIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Flanking;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(IIndicatorProvider.CardAppliesPowerToEnemy<FlankingPower>);
    }
}
