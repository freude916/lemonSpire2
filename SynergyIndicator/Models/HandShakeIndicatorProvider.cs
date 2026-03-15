using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.SynergyIndicator.Models;

public class HandShakeIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.HandShake;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        return handCards.Any(c => c.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly);
    }
}
