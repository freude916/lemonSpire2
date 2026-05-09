using System.Globalization;
using Godot;
using lemonSpire2.PlayerStateEx.RemoteFlash;
using lemonSpire2.SyncShop;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
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
///     支持鼠标点击：左键闪烁、卡牌/遗物右键详情、Alt+Click 发送物品
/// </summary>
public class ShopProvider : IPlayerPanelProvider
{
    private const int ItemsPerRow = 3;
    private const string GoldIconPath = "res://images/packed/sprite_fonts/gold_icon.png";
    private static Logger Log => PlayerPanelRegistry.Log;

    #region IPlayerPanelProvider Implementation

    public string Id => "shop";
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

        var unsubscribeInventory = SubscribeInventoryEvents(player, onUpdate, true);
        player.GoldChanged += OnGoldChanged;

        return () =>
        {
            Log.Debug($"UnsubscribeEvents for player {player.NetId}");
            unsubscribeInventory();
            player.GoldChanged -= OnGoldChanged;
        };

        void OnGoldChanged()
        {
            Log.Debug($"OnGoldChanged for player {player.NetId}");
            onUpdate();
        }
    }

    public Action SubscribeVisibilityEvents(Player player, Action onVisibilityChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onVisibilityChanged);
        return SubscribeInventoryEvents(player, onVisibilityChanged, false);
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        UiUtils.ClearChildren(content);
    }

    private static Action SubscribeInventoryEvents(Player player, Action onUpdate, bool shouldLog)
    {
        void OnInventoryUpdated(ulong netId)
        {
            if (shouldLog)
                Log.Debug($"OnInventoryUpdated: netId={netId}, player.NetId={player.NetId}");
            if (netId == player.NetId) onUpdate();
        }

        ShopManager.Instance.InventoryUpdated += OnInventoryUpdated;
        return () => ShopManager.Instance.InventoryUpdated -= OnInventoryUpdated;
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
        nEntry.GuiInput += @event => OnCardGuiInput(nEntry, player, card, @event);
        CardHoverTipHelper.BindCardHoverTip(nEntry, () => card, HoverTipAlignment.Right,
            () => StsUtil.IsInSelection(nEntry));
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

        holder.GuiInput += @event => OnRelicGuiInput(holder, player, relic, @event);

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

        holder.GuiInput += @event => OnPotionGuiInput(holder, player, potion, @event);
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

    private static void OnCardGuiInput(NDeckHistoryEntry clickedEntry, Player player, CardModel card, InputEvent @event)
    {
        if (StsUtil.IsInSelection(clickedEntry))
            return;

        switch (@event)
        {
            case InputEventMouseButton
            {
                Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right
            }:
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.ShopCard, card);
                PlayerPanelChatHelper.SendCardToChat(player, "LEMONSPIRE.chat.shopShare", card);
                Log.Debug($"Sent card to chat: {card.Title}");
                clickedEntry.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.ShopCard, card);
                clickedEntry.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                PlayerPanelChatHelper.OpenCardDetails(card);
                clickedEntry.GetViewport()?.SetInputAsHandled();
                break;
        }
    }

    private static void OnRelicGuiInput(Control clickedControl, Player player, RelicModel relic, InputEvent @event)
    {
        if (StsUtil.IsInSelection(clickedControl))
            return;

        switch (@event)
        {
            case InputEventMouseButton
            {
                Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right
            }:
                PlayerPanelChatHelper.SendRelicToChat(player, "LEMONSPIRE.chat.shopShare", relic);
                Log.Debug($"Sent relic to chat: {relic.Id.Entry}");
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.ShopRelic, relic);
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                PlayerPanelChatHelper.OpenRelicDetails(relic);
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
        }
    }

    private static void OnPotionGuiInput(Control clickedControl, Player player, PotionModel potion, InputEvent @event)
    {
        if (StsUtil.IsInSelection(clickedControl))
            return;

        switch (@event)
        {
            case InputEventMouseButton
            {
                Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right
            }:
                PlayerPanelChatHelper.SendPotionToChat(player, "LEMONSPIRE.chat.shopShare", potion);
                Log.Debug($"Sent potion to chat: {potion.Id.Entry}");
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.ShopPotion, potion);
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
        }
    }

    #endregion
}
