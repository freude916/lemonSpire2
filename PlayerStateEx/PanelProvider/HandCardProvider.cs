using Godot;
using lemonSpire2.PlayerStateEx.RemoteFlash;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     手牌显示提供者
///     在战斗中显示玩家的手牌，使用 NDeckHistoryEntry 组件
///     支持鼠标点击：左键闪烁、右键详情、Alt+Click 发送卡牌且不闪烁
/// </summary>
public class HandCardProvider : IPlayerPanelProvider
{
    private static Logger Log => PlayerPanelRegistry.Log;

    #region Event Handlers

    private static void OnEntryGuiInput(NDeckHistoryEntry entry, CardModel card, Player player, InputEvent @event)
    {
        if (StsUtil.IsInSelection(entry))
            return;

        switch (@event)
        {
            case InputEventMouseButton
            {
                Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right
            }:
                Log.Debug($"OnEntryAltClicked: {card.Title} ");
                PlayerPanelChatHelper.SendCardToChat(player, "LEMONSPIRE.chat.handCardShare", card);
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.HandCard, card);
                entry.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                Log.Debug($"OnEntryLeftClicked: {card.Title}");
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.HandCard, card);
                entry.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                Log.Debug($"OnEntryRightClicked: {card.Title}");
                PlayerPanelChatHelper.OpenHandCardDetails(player, card);
                entry.GetViewport()?.SetInputAsHandled();
                break;
        }
    }

    #endregion

    #region IPlayerPanelProvider Implementation

    public string Id => "hand_cards";
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
        UiUtils.ClearChildren(container);

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

        var groupCards = LemonSpireConfig.GroupHandCards;
        var groups = groupCards
            ? CardUtils.GroupCards(hand.Cards).Select(static group => group.AsEnumerable())
            : hand.Cards.Select(static card => new[] { card }.AsEnumerable());

        foreach (var group in groups)
        {
            var cardModels = group as CardModel[] ?? [.. group];
            var card = cardModels.First();
            var count = groupCards ? cardModels.Length : 1;
            var entry = NDeckHistoryEntry.Create(card, count);

            entry.GuiInput += @event => OnEntryGuiInput(entry, entry.Card, player, @event);

            CardHoverTipHelper.BindCardHoverTip(entry, () => card, HoverTipAlignment.Left,
                () => StsUtil.IsInSelection(entry));

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

        var creature = player.Creature;

        hand.ContentsChanged += onUpdate;
        creature.PowerApplied += OnPowerChanged;
        creature.PowerIncreased += OnPowerIncreased;
        creature.PowerDecreased += OnPowerDecreased;
        creature.PowerRemoved += OnPowerChanged;

        return () =>
        {
            hand.ContentsChanged -= onUpdate;
            creature.PowerApplied -= OnPowerChanged;
            creature.PowerIncreased -= OnPowerIncreased;
            creature.PowerDecreased -= OnPowerDecreased;
            creature.PowerRemoved -= OnPowerChanged;
        };

        void OnPowerChanged(PowerModel _)
        {
            onUpdate();
        }

        void OnPowerIncreased(PowerModel _, int __, bool ___)
        {
            onUpdate();
        }

        void OnPowerDecreased(PowerModel _, bool __)
        {
            onUpdate();
        }
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        UiUtils.ClearChildren(content);
    }

    #endregion
}
