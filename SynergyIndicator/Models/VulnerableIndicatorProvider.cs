using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace lemonSpire2.SynergyIndicator.Models;

public class VulnerableIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Vulnerable;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(IIndicatorProvider.CardAppliesPower<VulnerablePower>);
    }
}
