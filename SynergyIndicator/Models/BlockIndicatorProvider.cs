using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using IndicatorType = lemonSpire2.SynergyIndicator.IndicatorRegistry.IndicatorType;

namespace lemonSpire2.SynergyIndicator.Models;

public class BlockIndicatorProvider : IIndicatorProvider
{
    public IndicatorType Type => IndicatorType.Block;

    public bool ShouldShow(IEnumerable<CardModel> handCards)
    {
        var cardModels = handCards as CardModel[] ?? [.. handCards];
        return cardModels.Any(card => IIndicatorProvider.CardHasVarToAllies(card, nameof(DynamicVarSet.Block))) ||
               cardModels.Any(card => card.Id.Entry == "DEMONIC_SHIELD")
            ;
    }
}
