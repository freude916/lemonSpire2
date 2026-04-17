using lemonSpire2.Chat.Input;
using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Message;
using Xunit;

namespace lemonSpire2.Tests.Chat.Input;

public sealed class ChatCompletionAnalyzerTests
{
    [Fact]
    public void TryAnalyze_ShouldMatchMentionContext()
    {
        var services = CreateServices(
        [
            new MentionTarget("Alice", 42, () => new RichTextSegment { Text = "<alice>" })
            {
                MentionText = "Alice_1"
            }
        ]);

        Assert.True(services.CompletionAnalyzer.TryAnalyze("hi @Ali", "hi @Ali".Length, out var session));
        Assert.Equal(3, session.ReplaceStart);
        Assert.Equal(4, session.ReplaceLength);

        var items = session.Provider.GetItems(session.Query);
        Assert.Contains(items, static item => item.InsertText == "@Alice_1");
    }

    [Fact]
    public void TryAnalyze_ShouldMatchSlashCommandContext()
    {
        var services = CreateServices();

        Assert.True(services.CompletionAnalyzer.TryAnalyze("/", 1, out var session));

        var items = session.Provider.GetItems(session.Query);
        Assert.Contains(items, static item => item.InsertText == "/help ");
    }

    [Fact]
    public void TryAnalyze_ShouldMatchInlineReferenceContext()
    {
        var services = CreateServices();

        Assert.True(services.CompletionAnalyzer.TryAnalyze("<car", "<car".Length, out var session));
        Assert.Equal(0, session.ReplaceStart);
        Assert.Equal(4, session.ReplaceLength);

        var items = session.Provider.GetItems(session.Query);
        Assert.Contains(items, static item => item.InsertText == "<card:");
    }

    [Fact]
    public void TryAnalyze_ShouldReturnFalse_WhenCaretIsNotInKnownContext()
    {
        var services = CreateServices();

        Assert.False(services.CompletionAnalyzer.TryAnalyze("hello world", "hello world".Length, out _));
    }

    private static ChatInputServices CreateServices(IReadOnlyList<MentionTarget>? mentionTargets = null)
    {
        return new ChatInputServices(
            mentionTargetsOverride: mentionTargets,
            [
                new FakeInlineReference("card")
            ]);
    }

    private sealed class FakeInlineReference(string typeName)
        : IChatInlineReference
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
}
