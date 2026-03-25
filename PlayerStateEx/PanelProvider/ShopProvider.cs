using System.Globalization;
using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.SyncShop;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     商店显示提供者
///     显示玩家商店中的卡牌、遗物、药水
///     卡牌：横向布局，左价格右卡牌
///     遗物/药水：网格布局，物品在上价格在下，一行三个
///     支持 Alt+Click 发送物品到聊天
/// </summary>
public class ShopProvider : IPlayerPanelProvider
{
    private const int ItemsPerRow = 3;
    private const string GoldIconPath = "res://images/packed/sprite_fonts/gold_icon.png";
    private static Logger Log => PlayerPanelRegistry.Log;

    #region IPlayerPanelProvider Implementation

    public string ProviderId => "shop";
    public int Priority => 30;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.shop").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return ShopManager.Instance.HasInventory(player.NetId);
    }

    public Control CreateContent(Player player)
    {
        var container = new VBoxContainer
        {
            Name = "ShopContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 8);

        return container;
    }

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (content is not VBoxContainer container) return;

        UiUtils.ClearChildren(container);

        // 显示对方金币
        container.AddChild(CreateGoldRow(player));

        var items = ShopManager.Instance.GetInventory(player.NetId);
        if (items == null || items.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = new LocString("gameplay_ui", "LEMONSPIRE.shop.empty").GetFormattedText(),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            container.AddChild(emptyLabel);
            return;
        }

        // 分组显示
        var cards = items.Where(i => i is { Type: ShopItemType.Card, IsStocked: true }).ToList();
        var relics = items.Where(i => i is { Type: ShopItemType.Relic, IsStocked: true }).ToList();
        var potions = items.Where(i => i is { Type: ShopItemType.Potion, IsStocked: true }).ToList();

        // 卡牌：横向布局
        foreach (var card in cards)
            AddCardRow(container, player, card);

        // 遗物：网格布局，物品在上价格在下
        if (relics.Count > 0)
            AddItemGrid(container, player, relics, AddRelicItem);

        // 药水：网格布局，物品在上价格在下
        if (potions.Count > 0)
            AddItemGrid(container, player, potions, AddPotionItem);

        Log.Debug(
            $"Updated content for player {player.NetId}: {cards.Count} cards, {relics.Count} relics, {potions.Count} potions");
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);

        Log.Debug($"SubscribeEvents for player {player.NetId}");

        ShopManager.Instance.InventoryUpdated += OnInventoryUpdated;
        player.GoldChanged += OnGoldChanged;

        return () =>
        {
            Log.Debug($"UnsubscribeEvents for player {player.NetId}");
            ShopManager.Instance.InventoryUpdated -= OnInventoryUpdated;
            player.GoldChanged -= OnGoldChanged;
        };

        void OnGoldChanged()
        {
            Log.Debug($"OnGoldChanged for player {player.NetId}");
            onUpdate();
        }

        void OnInventoryUpdated(ulong netId)
        {
            Log.Debug($"OnInventoryUpdated: netId={netId}, player.NetId={player.NetId}");
            if (netId == player.NetId) onUpdate();
        }
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        UiUtils.ClearChildren(content);
    }

    #endregion

    #region UI Creation

    private static HBoxContainer CreateGoldRow(Player player)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);

        var goldIcon = new TextureRect
        {
            Texture = GD.Load<Texture2D>(GoldIconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(16, 16),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        row.AddChild(goldIcon);

        var goldLabel = new Label
        {
            Text = player.Gold.ToString(CultureInfo.InvariantCulture),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        goldLabel.AddThemeColorOverride("font_color", StsColors.gold);
        goldLabel.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(goldLabel);

        var titleLabel = new Label
        {
            Text = new LocString("gameplay_ui", "LEMONSPIRE.shop.gold").GetFormattedText(),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(titleLabel);

        return row;
    }

    /// <summary>
    ///     卡牌：横向布局，左价格右卡牌
    /// </summary>
    private static void AddCardRow(VBoxContainer container, Player player, ShopItemEntry entry)
    {
        var card = StsUtil.ResolveModel<CardModel>(entry.ModelId);
        if (card == null) return;

        if (entry.UpgradeLevel > 0 && card.CurrentUpgradeLevel < entry.UpgradeLevel)
            card = card.ToMutable();

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);

        var priceLabel = CreatePriceLabel(player, entry);
        row.AddChild(priceLabel);

        var nEntry = NDeckHistoryEntry.Create(card, 1);
        nEntry.Connect(NDeckHistoryEntry.SignalName.Clicked,
            Callable.From<NDeckHistoryEntry>(_ => OnCardClicked(player, entry, card)));
        CardHoverTipHelper.BindCardHoverTip(nEntry, () => card, HoverTipAlignment.Right);
        row.AddChild(nEntry);

        container.AddChild(row);
    }

    /// <summary>
    ///     网格布局：物品在上价格在下，一行多个
    /// </summary>
    private static void AddItemGrid(VBoxContainer container, Player player,
        List<ShopItemEntry> items, Action<Player, ShopItemEntry, HBoxContainer> addItem)
    {
        for (var i = 0; i < items.Count; i += ItemsPerRow)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            row.AddThemeConstantOverride("separation", 8);

            // 先加入场景树，让子节点的 _Ready() 能正常执行
            container.AddChild(row);

            for (var j = 0; j < ItemsPerRow && i + j < items.Count; j++)
                addItem(player, items[i + j], row);
        }
    }

    /// <summary>
    ///     添加遗物项到行：物品在上，价格在下
    /// </summary>
    private static void AddRelicItem(Player player, ShopItemEntry entry, HBoxContainer row)
    {
        var relic = StsUtil.ResolveModel<RelicModel>(entry.ModelId);
        if (relic == null) return;

        var holder = NRelicBasicHolder.Create(relic.ToMutable());
        if (holder == null) return;

        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
        };
        container.AddThemeConstantOverride("separation", 2);

        // 物品在上（先加入场景树）
        container.AddChild(holder);

        // 价格在下
        var priceLabel = CreatePriceLabel(player, entry, true);
        container.AddChild(priceLabel);

        // 连接事件
        holder.Connect(NClickableControl.SignalName.Released,
            Callable.From<Variant>(_ => OnRelicClicked(player, entry, relic)));

        row.AddChild(container);
    }

    /// <summary>
    ///     添加药水项到行：物品在上，价格在下
    /// </summary>
    private static void AddPotionItem(Player player, ShopItemEntry entry, HBoxContainer row)
    {
        var potion = StsUtil.ResolveModel<PotionModel>(entry.ModelId);
        if (potion == null) return;

        var nPotion = NPotion.Create(potion.ToMutable());
        if (nPotion == null) return;

        var holder = NPotionHolder.Create(false);

        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
        };
        container.AddThemeConstantOverride("separation", 2);

        // 物品在上（先加入场景树）
        row.AddChild(container);
        container.AddChild(holder);
        holder.AddPotion(nPotion);
        nPotion.Position = Vector2.Zero;
        // 价格在下
        var priceLabel = CreatePriceLabel(player, entry, true);
        container.AddChild(priceLabel);

        // 连接事件
        holder.Connect(NClickableControl.SignalName.Released,
            Callable.From<Variant>(_ => OnPotionClicked(player, entry, potion)));
    }

    private static Label CreatePriceLabel(Player player, ShopItemEntry entry, bool centered = false)
    {
        Label priceLabel;
        if (centered)
            priceLabel = new Label
            {
                Text = $"{entry.Cost}g",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
        else
            priceLabel = new Label
            {
                Text = $"{entry.Cost}g",
                CustomMinimumSize = new Vector2(36, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        if (player.Gold < entry.Cost)
            priceLabel.AddThemeColorOverride("font_color", StsColors.red);
        else if (entry.IsOnSale)
            priceLabel.AddThemeColorOverride("font_color", StsColors.green);
        else
            priceLabel.AddThemeColorOverride("font_color", StsColors.cream);

        priceLabel.AddThemeFontSizeOverride("font_size", 14);
        return priceLabel;
    }

    #endregion

    #region Event Handlers

    private static void OnCardClicked(Player player, ShopItemEntry entry, CardModel card)
    {
        if (!Input.IsKeyPressed(Key.Alt)) return;
        var segment = new TooltipSegment { Tooltip = CardTooltip.FromModel(card) };
        PlayerPanelChatHelper.SendPlayerItemToChat(player, "LEMONSPIRE.chat.shopShare", segment);
        Log.Debug($"Sent card to chat: {card.Title}");
    }

    private static void OnRelicClicked(Player player, ShopItemEntry entry, RelicModel relic)
    {
        if (!Input.IsKeyPressed(Key.Alt)) return;
        var segment = new TooltipSegment { Tooltip = RelicTooltip.FromModel(relic) };
        PlayerPanelChatHelper.SendPlayerItemToChat(player, "LEMONSPIRE.chat.shopShare", segment);
        Log.Debug($"Sent relic to chat: {relic.Id.Entry}");
    }

    private static void OnPotionClicked(Player player, ShopItemEntry entry, PotionModel potion)
    {
        if (!Input.IsKeyPressed(Key.Alt)) return;
        var segment = new TooltipSegment { Tooltip = PotionTooltip.FromModel(potion) };
        PlayerPanelChatHelper.SendPlayerItemToChat(player, "LEMONSPIRE.chat.shopShare", segment);
        Log.Debug($"Sent potion to chat: {potion.Id.Entry}");
    }

    #endregion
}
