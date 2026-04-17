using lemonSpire2.Chat.Input;
using Xunit;

namespace lemonSpire2.Tests.Chat.Input;

public sealed class ChatInputInsertionTests
{
    [Fact]
    public void InsertToken_ShouldPadSpaces_WhenInsertedBetweenWords()
    {
        var result = ChatInputInsertion.InsertToken("hi there", 2, "<card:CLASH>");

        Assert.Equal("hi <card:CLASH> there", result.Text);
        Assert.Equal("hi <card:CLASH>".Length, result.CaretColumn);
    }

    [Fact]
    public void InsertToken_ShouldAppendTrailingSpace_WhenInsertedAtEnd()
    {
        var result = ChatInputInsertion.InsertToken("hi", 2, "<card:CLASH>");

        Assert.Equal("hi <card:CLASH> ", result.Text);
        Assert.Equal("hi <card:CLASH> ".Length, result.CaretColumn);
    }
}
