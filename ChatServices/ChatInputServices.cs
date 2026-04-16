using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Input.Parsing;
using lemonSpire2.Chat.Input.Registry;
using lemonSpire2.Chat.Input.Service.Bracket;
using lemonSpire2.Chat.Input.Service.Mention;
using lemonSpire2.Chat.Message;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Chat.Input;

public sealed class ChatInputServices
{
    public ChatInputServices()
    {
        var mentionTargetGetter = GetMentionTargets;

        InlineReferenceRegistry = new ChatInlineReferenceRegistry();
        CompletionLeaderRegistry = new ChatCompletionLeaderRegistry();
        SubmitTokenHandlerRegistry = new ChatSubmitTokenHandlerRegistry();

        InlineReferenceRegistry.Register(new CardInlineReferenceType());
        SubmitTokenHandlerRegistry.Register(new MentionSubmitTokenHandler(mentionTargetGetter));
        SubmitTokenHandlerRegistry.Register(new BracketSubmitTokenHandler(InlineReferenceRegistry));

        Parser = new ChatInputSubmitParser(SubmitTokenHandlerRegistry);

        CompletionLeaderRegistry.Register(
            new MentionCompletionLeader(new MentionCompletionProvider(mentionTargetGetter)));
        CompletionLeaderRegistry.Register(
            new BracketCompletionLeader(new InlineReferenceCompletionProvider(InlineReferenceRegistry)));
    }

    public ChatInlineReferenceRegistry InlineReferenceRegistry { get; }
    public ChatCompletionLeaderRegistry CompletionLeaderRegistry { get; }
    public ChatSubmitTokenHandlerRegistry SubmitTokenHandlerRegistry { get; }
    public ChatInputSubmitParser Parser { get; }

    private static IReadOnlyList<MentionTarget> GetMentionTargets()
    {
        var players = RunManager.Instance.State?.Players ?? [];
        return [.. players.Select(ToMentionTarget)];
    }

    private static MentionTarget ToMentionTarget(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new MentionTarget(StsUtil.GetPlayerNameFromNetId(player.NetId), () => EntitySegment.FromPlayer(player));
    }
}
