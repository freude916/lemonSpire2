using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Command;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Input.Parsing;
using lemonSpire2.Chat.Input.Registry;
using lemonSpire2.Chat.Input.Service.Bracket;
using lemonSpire2.Chat.Input.Service.Command;
using lemonSpire2.Chat.Input.Service.Mention;
using lemonSpire2.Chat.Message;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Chat.Input;

public sealed class ChatInputServices
{
    public ChatInputServices(
        IReadOnlyList<MentionTarget>? mentionTargetsOverride = null,
        IReadOnlyList<IChatInlineReference>? inlineReferences = null)
    {
        Func<IReadOnlyList<MentionTarget>> mentionTargetGetter = mentionTargetsOverride is null
            ? GetMentionTargets
            : () => mentionTargetsOverride;

        // 这里负责把“completion / submit parser / command”三条输入链路装配到一起。
        InlineReferenceRegistry = new ChatInlineReferenceRegistry();
        CompletionAnalyzer = new ChatCompletionAnalyzerRegistry();
        SubmitTokenHandlerRegistry = new ChatSubmitTokenHandlerRegistry();
        CommandRegistry = new ChatCmdRegistry();

        RegisterReferences(inlineReferences);

        SubmitTokenHandlerRegistry.Register(new MentionSubmitTokenHandler(mentionTargetGetter));
        SubmitTokenHandlerRegistry.Register(new BracketSubmitTokenHandler(InlineReferenceRegistry));

        Parser = new ChatInputSubmitParser(SubmitTokenHandlerRegistry);
        DefaultChatCmds.RegisterDefaults(CommandRegistry, mentionTargetGetter, GetLocalNetId, Parser.Parse);
        CommandProcessor = new ChatCmdProcessor(CommandRegistry);

        RegisterCompletionServices(mentionTargetGetter);
    }

    public ChatInlineReferenceRegistry InlineReferenceRegistry { get; }
    public ChatCompletionAnalyzerRegistry CompletionAnalyzer { get; }
    public ChatSubmitTokenHandlerRegistry SubmitTokenHandlerRegistry { get; }
    public ChatCmdRegistry CommandRegistry { get; }
    public ChatCmdProcessor CommandProcessor { get; }
    public ChatInputSubmitParser Parser { get; }

    private void RegisterReferences()
    {
        InlineReferenceRegistry.Register(new CardInlineReference());
        InlineReferenceRegistry.Register(new PotionInlineReference());
        InlineReferenceRegistry.Register(new RelicInlineReference());
    }

    private void RegisterReferences(IReadOnlyList<IChatInlineReference>? inlineReferences)
    {
        RegisterReferences();

        if (inlineReferences == null) return;

        foreach (var inlineReference in inlineReferences)
            InlineReferenceRegistry.Register(inlineReference);
    }

    private void RegisterCompletionServices(Func<IReadOnlyList<MentionTarget>> mentionTargetGetter)
    {
        ArgumentNullException.ThrowIfNull(mentionTargetGetter);

        CompletionAnalyzer.Register(new SlashCommandCompletionAnalyzer(CommandRegistry));
        CompletionAnalyzer.Register(new MentionCompletionAnalyzer(new MentionCompletionProvider(mentionTargetGetter)));
        CompletionAnalyzer.Register(
            new InlineReferenceCompletionAnalyzer(new InlineReferenceCompletionProvider(InlineReferenceRegistry)));
    }

    private static IReadOnlyList<MentionTarget> GetMentionTargets()
    {
        var players = RunManager.Instance.State?.Players ?? [];
        // alias 在输入阶段必须稳定且唯一，否则 /w 这类命令无法只靠文本可靠回解析到玩家。
        var aliases = MentionAliasService.CreateAliases(
            players.Select(player =>
                new MentionAliasSource(StsUtil.GetPlayerNameFromNetId(player.NetId), player.NetId)));
        return [.. players.Select(player => ToMentionTarget(player, aliases[player.NetId]))];
    }

    private static ulong GetLocalNetId()
    {
        return RunManager.Instance.NetService.NetId;
    }

    private static MentionTarget ToMentionTarget(Player player, string mentionText)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new MentionTarget(StsUtil.GetPlayerNameFromNetId(player.NetId), player.NetId,
            () => EntitySegment.FromPlayer(player))
        {
            MentionText = mentionText
        };
    }
}
