using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.SyncReward;
using lemonSpire2.Tooltips;
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
///     卡牌奖励显示提供者
///     显示其他玩家当前可选择的卡牌奖励
///     支持分组显示、滚动条、Alt+Click 发送
/// </summary>
public class CardRewardProvider : IPlayerPanelProvider
{
    private static Logger Log => PlayerPanelRegistry.Log;

    #region Event Handlers

    private static void OnCardClicked(Player player, CardModel card)
    {
        if (!Input.IsKeyPressed(Key.Alt)) return;

        var segment = new TooltipSegment
        {
            Tooltip = CardTooltip.FromModel(card)
        };
        PlayerPanelChatHelper.SendPlayerItemToChat(player, "LEMONSPIRE.chat.cardRewardShare", segment);
        Log.Debug($"Sent card to chat: {card.Title}");
    }

    #endregion

    #region IPlayerPanelProvider

    public string ProviderId => "card_rewards";
    public int Priority => 15;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.cardRewards").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return CardRewardManager.Instance.HasRewards(player.NetId);
    }

    public Control CreateContent(Player player)
    {
        var container = new VBoxContainer
        {
            Name = "CardRewardsContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 8);

        return container;
    }

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (content is not VBoxContainer container) return;

        // 清除现有内容
        UiUtils.ClearChildren(container);

        var groups = CardRewardManager.Instance.GetGroups(player.NetId);
        if (groups.Count == 0)
        {
            Log.Error($"Should not happen! at least one reward in every combat, got 0 in player {player.NetId}");
            return;
        }

        foreach (var group in groups)
        {
            var groupPanel = CreateGroupPanel(player, group);
            container.AddChild(groupPanel);
        }

        Log.Debug($"Updated content for player {player.NetId}: {groups.Count} groups");
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);

        void OnRewardsUpdated(ulong netId)
        {
            if (netId == player.NetId) onUpdate();
        }

        CardRewardManager.Instance.RewardsUpdated += OnRewardsUpdated;
        return () => CardRewardManager.Instance.RewardsUpdated -= OnRewardsUpdated;
    }

    public Action SubscribeVisibilityEvents(Player player, Action onVisibilityChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onVisibilityChanged);

        void OnRewardsUpdated(ulong netId)
        {
            if (netId == player.NetId) onVisibilityChanged();
        }

        CardRewardManager.Instance.RewardsUpdated += OnRewardsUpdated;
        return () => CardRewardManager.Instance.RewardsUpdated -= OnRewardsUpdated;
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        UiUtils.ClearChildren(content);
    }

    #endregion

    #region UI Creation

    private static PanelContainer CreateGroupPanel(Player player, CardRewardGroup group)
    {
        var panel = new PanelContainer
        {
            Name = $"RewardGroup_{group.GroupId}"
        };

        // 添加边框样式
        var styleBox = new StyleBoxFlat
        {
            BorderColor = new Color(0.4f, 0.4f, 0.45f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            BgColor = new Color(0.15f, 0.15f, 0.18f, 0.8f),
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        panel.AddThemeStyleboxOverride("panel", styleBox);

        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 2);
        panel.AddChild(container);

        // 组标题（显示来源类型）
        // 暂时注释掉，只有"奖励"和"特殊奖励"两种类型，区分度不大，且占用空间
        /*var sourceText = group.Source switch
        {
            CardRewardSourceType.Special => new LocString("gameplay_ui", "LEMONSPIRE.cardRewards.special").GetFormattedText(),
            _ => new LocString("gameplay_ui", "LEMONSPIRE.cardRewards.normal").GetFormattedText()
        };

        var headerLabel = new Label
        {
            Text = sourceText,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        headerLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        headerLabel.AddThemeFontSizeOverride("font_size", 16);
        container.AddChild(headerLabel);*/

        // 卡牌列（竖排）
        var cardColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        cardColumn.AddThemeConstantOverride("separation", 2);
        container.AddChild(cardColumn);

        foreach (var cardEntry in group.Cards)
        {
            var cardControl = CreateCardControl(player, cardEntry);
            cardColumn.AddChild(cardControl);
        }

        return panel;
    }

    private static Control CreateCardControl(Player player, CardEntry cardEntry)
    {
        var card = StsUtil.ResolveModel<CardModel>(cardEntry.ModelId);
        if (card == null)
        {
            var brokenLabel = new Label
            {
                Text = $"[{cardEntry.ModelId}]",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            brokenLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.2f, 0.2f));
            return brokenLabel;
        }

        // 应用升级等级
        if (cardEntry.UpgradeLevel > 0 && card.CurrentUpgradeLevel < cardEntry.UpgradeLevel)
        {
            card = card.ToMutable();
            card._currentUpgradeLevel = cardEntry.UpgradeLevel;
        }


        var entry = NDeckHistoryEntry.Create(card, 1);

        // 订阅点击事件
        entry.Connect(NDeckHistoryEntry.SignalName.Clicked,
            Callable.From<NDeckHistoryEntry>(_ => OnCardClicked(player, card)));

        // 添加悬浮提示
        CardHoverTipHelper.BindCardHoverTip(entry, () => card, HoverTipAlignment.Left);

        return entry;
    }

    #endregion
}
