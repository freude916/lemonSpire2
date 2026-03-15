using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.QoL;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     手牌显示提供者
///     在战斗中显示玩家的手牌，使用 NDeckHistoryEntry 组件
///     支持 Click 打开详情、Alt+Click 发送卡牌
/// </summary>
public class HandCardProvider : IPlayerPanelProvider
{
    private static Logger Log => PlayerPanelRegistry.Log;

    #region Event Handlers

    private static void OnEntryClicked(CardModel card, Player player)
    {
        Log.Debug($"OnEntryClicked: {card.Title}, Alt={Input.IsKeyPressed(Key.Alt)}");

        // 每次点击时重新获取手牌列表，确保是最新的
        var cards = player.PlayerCombatState?.Hand.Cards.ToList() ?? new List<CardModel>();

        if (Input.IsKeyPressed(Key.Alt))
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

    #endregion

    #region IPlayerPanelProvider Implementation

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

        var hand = player.PlayerCombatState?.Hand;
        if (hand == null)
        {
            Log.Debug("Hand is null for player");
            return;
        }

        // 清除现有内容
        ProviderUtils.ClearChildren(container);

        // 添加手牌数量显示
        var cardCount = hand.Cards.Count;
        var handLabel = new LocString("gameplay_ui", "LEMONSPIRE.panel.hand").GetFormattedText();
        var countLabel = new Label
        {
            Text = $"{handLabel}: {cardCount}/{MaxHandSize}",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        countLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        countLabel.AddThemeFontSizeOverride("font_size", 16);
        container.AddChild(countLabel);

        // 分组：相同卡牌合并显示
        var groups = CardUtils.GroupCards(hand.Cards);

        foreach (var group in groups)
        {
            var card = group.First();
            var count = group.Count();
            var entry = NDeckHistoryEntry.Create(card, count);

            entry.Connect(NDeckHistoryEntry.SignalName.Clicked,
                Callable.From<NDeckHistoryEntry>(e => OnEntryClicked(e.Card, player)));

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
            Log.Debug("SubscribeEvents: Hand is null");
            return null;
        }

        hand.ContentsChanged += onUpdate;
        return () => hand.ContentsChanged -= onUpdate;
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ProviderUtils.ClearChildren(content);
    }

    #endregion
}
