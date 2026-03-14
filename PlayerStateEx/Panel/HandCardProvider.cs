using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.QoL;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace lemonSpire2.PlayerStateEx.Panel;

/// <summary>
///     手牌显示提供者
///     在战斗中显示玩家的手牌，使用 NDeckHistoryEntry 组件
///     支持 Click 打开详情、Alt+Click 发送卡牌
/// </summary>
public class HandCardProvider : IPlayerPanelProvider
{
    public string ProviderId => "hand_cards";
    public int Priority => 10;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.hand").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.PlayerCombatState?.Hand != null;
    }

    public Control CreateContent(Player player)
    {
        var container = new VBoxContainer
        {
            Name = "HandCardsContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 2);

        // 不在这里调用 UpdateContent，等待加入场景树后再调用
        return container;
    }

    private const int MaxHandSize = 10;

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (content is not VBoxContainer container) return;

        // 清除现有内容
        ProviderUtils.ClearChildren(container);

        var hand = player.PlayerCombatState?.Hand;
        if (hand == null)
        {
            MainFile.Logger.Debug("[HandCardProvider] Hand is null for player");
            return;
        }

        var cardCount = hand.Cards.Count;
        MainFile.Logger.Debug($"[HandCardProvider] Updating content, hand has {cardCount} cards");

        // 添加手牌数量显示
        var handLabel = new LocString("gameplay_ui", "LEMONSPIRE.panel.hand").GetFormattedText();
        var countLabel = new Label
        {
            Text = $"{handLabel}: {cardCount}/{MaxHandSize}",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        countLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        countLabel.AddThemeFontSizeOverride("font_size", 11);
        container.AddChild(countLabel);

        // 分组：相同卡牌合并显示
        var groups = hand.Cards
            .GroupBy(c => new CardGroupKey(c))
            .OrderByDescending(g => g.Key.Rarity)
            .ThenBy(g => g.Key.Title);

        foreach (var group in groups)
        {
            var card = group.First();
            var count = group.Count();
            var entry = NDeckHistoryEntry.Create(card, count);

            // 使用 Connect 方法订阅点击事件（与游戏源码一致）
            entry.Connect(NDeckHistoryEntry.SignalName.Clicked,
                Callable.From<NDeckHistoryEntry>(e => OnEntryClicked(e.Card, player)));

            // 添加悬浮提示功能
            CardHoverTipHelper.BindCardHoverTip(entry, () => card, HoverTipAlignment.Right);

            container.AddChild(entry);
        }
    }

    public Action? SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);
        var hand = player.PlayerCombatState?.Hand;
        if (hand == null)
        {
            MainFile.Logger.Debug("[HandCardProvider] SubscribeEvents: Hand is null");
            return null;
        }

        void OnHandChanged()
        {
            MainFile.Logger.Debug("[HandCardProvider] Hand ContentsChanged event triggered");
            onUpdate();
        }

        hand.ContentsChanged += OnHandChanged;
        return () => hand.ContentsChanged -= OnHandChanged;
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ProviderUtils.ClearChildren(content);
    }

    private static void OnEntryClicked(CardModel card, Player player)
    {
        MainFile.Logger.Debug($"[HandCardProvider] OnEntryClicked: {card.Title}, Alt={ProviderUtils.IsAltClick()}");

        // 每次点击时重新获取手牌列表，确保是最新的
        var cards = player.PlayerCombatState?.Hand?.Cards.ToList() ?? new List<CardModel>();

        if (ProviderUtils.IsAltClick())
        {
            // Alt+Click: 发送卡牌到聊天
            var segment = new TooltipSegment
            {
                Tooltip = CardTooltip.FromModel(card)
            };

            ProviderUtils.SendToChat(segment);
        }
        else
        {
            // 普通点击: 打开卡牌详情界面
            var index = cards.IndexOf(card);
            if (index >= 0) NGame.Instance?.GetInspectCardScreen().Open(cards, index);
        }
    }

    /// <summary>
    ///     卡牌分组键（相同ID、升级等级、附魔的卡牌视为相同）
    /// </summary>
    private readonly record struct CardGroupKey(CardModel Card)
    {
        public readonly CardRarity Rarity = Card.Rarity;
        public readonly string Title = Card.Title;

        public bool Equals(CardGroupKey other)
        {
            return Card.Id.Equals(other.Card.Id) &&
                   Card.CurrentUpgradeLevel == other.Card.CurrentUpgradeLevel &&
                   Card.Enchantment?.Id == other.Card.Enchantment?.Id &&
                   Card.Enchantment?.Amount == other.Card.Enchantment?.Amount;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Card.Id, Card.CurrentUpgradeLevel, Card.Enchantment?.Id, Card.Enchantment?.Amount);
        }
    }
}
