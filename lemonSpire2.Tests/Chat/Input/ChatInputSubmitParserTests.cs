using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Input.Parsing;
using lemonSpire2.Chat.Input.Registry;
using lemonSpire2.Chat.Input.Service.Bracket;
using lemonSpire2.Chat.Input.Service.Mention;
using lemonSpire2.Chat.Message;
using Xunit;

namespace lemonSpire2.Tests.Chat.Input;

public sealed class ChatInputSubmitParserTests
{
    [Fact]
    public void Parse_ShouldFallbackToPlainText_ForUnknownSlashCommand()
    {
        var parser = new ChatInputSubmitParser(new ChatSubmitTokenHandlerRegistry());

        var result = parser.Parse("/foobar hi");

        var composition = result;
        var text = Assert.IsType<RichTextSegment>(Assert.Single(composition));
        Assert.IsType<RichTextSegment>(text);
        Assert.Equal("/foobar hi", text.Render());
    }

    [Fact]
    public void Parse_ShouldSplitMentionAndInlineReference_WhenTargetsResolve()
    {
        var inlineRegistry = new ChatInlineReferenceRegistry();
        inlineRegistry.Register(new FakeInlineReference("card"));
        var tokenHandlers = new ChatSubmitTokenHandlerRegistry();
        tokenHandlers.Register(new MentionSubmitTokenHandler(() => GetMentionTargets("Alice", 42)));
        tokenHandlers.Register(new BracketSubmitTokenHandler(inlineRegistry));
        var parser = new ChatInputSubmitParser(tokenHandlers);

        var result = parser.Parse("看这个 @Alice <card:CLASH>");

        var composition = result;
        Assert.Collection(composition,
            segment =>
            {
                var text = Assert.IsType<RichTextSegment>(segment);
                Assert.Equal("看这个 ", text.Text);
            },
            segment => Assert.IsType<EntitySegment>(segment),
            segment =>
            {
                var text = Assert.IsType<RichTextSegment>(segment);
                Assert.Equal(" ", text.Text);
            },
            segment =>
            {
                var text = Assert.IsType<RichTextSegment>(segment);
                Assert.Equal("<card:CLASH>", text.Text);
            });
    }

    [Fact]
    public void Parse_ShouldUseRegisteredTokenHandler_ForCustomTrigger()
    {
        var tokenHandlers = new ChatSubmitTokenHandlerRegistry();
        tokenHandlers.Register(new HashSubmitTokenHandler());
        var parser = new ChatInputSubmitParser(tokenHandlers);

        var result = parser.Parse("before #tag after");

        var composition = result;
        Assert.Collection(composition,
            segment => Assert.Equal("before ", Assert.IsType<RichTextSegment>(segment).Text),
            segment => Assert.Equal("<hash:tag>", Assert.IsType<RichTextSegment>(segment).Text),
            segment => Assert.Equal(" after", Assert.IsType<RichTextSegment>(segment).Text));
    }

    [Fact]
    public void Parse_ShouldResolveMention_WhenDisplayNameContainsSpaces()
    {
        var tokenHandlers = new ChatSubmitTokenHandlerRegistry();
        tokenHandlers.Register(new MentionSubmitTokenHandler(() => GetMentionTargets("Alice Bob", 7)));
        var parser = new ChatInputSubmitParser(tokenHandlers);

        var result = parser.Parse("hi @Alice_Bob");

        var composition = result;
        Assert.Collection(composition,
            segment => Assert.Equal("hi ", Assert.IsType<RichTextSegment>(segment).Text),
            segment => Assert.IsType<EntitySegment>(segment));
    }

    private static IReadOnlyList<MentionTarget> GetMentionTargets(string displayName, ulong playerNetId)
    {
        return
        [
            new MentionTarget(displayName, playerNetId, () => new EntitySegment
            {
                Kind = EntitySegment.EntityKind.Player,
                DisplayNameKind = EntitySegment.NameKind.Plain,
                PlayerNetId = playerNetId,
                DisplayName = displayName
            })
        ];
    }


    private sealed class FakeInlineReference(string typeName) : IChatInlineReference
    {
        public string TypeName => typeName;

        public IReadOnlyList<ChatCompletionItem> GetCompletions(string query)
        {
            return
            [
                new ChatCompletionItem($"{query}-display", $"<{typeName}:{query}>")
            ];
        }

        public bool TryResolve(string payload, out IMsgSegment segment)
        {
            segment = new RichTextSegment { Text = $"<{typeName}:{payload}>" };
            return true;
        }
    }

    private sealed class HashSubmitTokenHandler : IChatSubmitTokenHandler
    {
        public char TriggerChar => '#';

        public bool TryParse(string text, int start, out IMsgSegment segment, out int length)
        {
            segment = null!;
            length = 0;

            if (start + 1 >= text.Length)
                return false;

            var end = text.IndexOf(' ', start);
            if (end < 0)
                end = text.Length;

            var payload = text[(start + 1)..end];
            if (string.IsNullOrEmpty(payload))
                return false;

            segment = new RichTextSegment { Text = $"<hash:{payload}>" };
            length = end - start;
            return true;
        }
    }
}
