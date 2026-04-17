using lemonSpire2.Chat.Input.Service.Bracket;
using Xunit;

namespace lemonSpire2.Tests.Chat.Input;

public sealed class CardInlineReferenceTests
{
    [Fact]
    public void BuildCompletionItems_ShouldReturnAllMatchesWithoutTwentyItemCap()
    {
        var candidates = Enumerable.Range(1, 25)
            .Select(index => new CardCompletionCandidate($"Card {index}", $"card_{index:D2}"))
            .ToArray();

        var items = CardInlineReference.BuildCompletionItems(candidates, "Card");

        Assert.Equal(25, items.Count);
        Assert.Equal("<card:card_01>", items[0].InsertText);
        Assert.Equal("<card:card_25>", items[^1].InsertText);
    }
}
