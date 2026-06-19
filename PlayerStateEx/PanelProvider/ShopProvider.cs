using System.Globalization;
using Godot;
using lemonSpire2.PlayerStateEx.RemoteFlash;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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

    internal static event Action<ulong>? ShopUpdated;

    /// <summary>
    ///     由 ShopProviderPatch 调用，通知指定玩家的商店数据已变化
    /// </summary>
    internal static void NotifyShopUpdated(ulong netId)
    {
        ShopUpdated?.Invoke(netId);
    }

    /// <summary>
    ///     由 ShopProviderPatch 调用，触发商店数据刷新
    /// </summary>
    internal static void RequestRefresh()
    {
        var room = NMerchantRoom.Instance?.Room;
        if (room == null || room.Inventories.Count == 0)
            return;

        foreach (var inventory in room.Inventories)
        {
            Log.Debug(
                $"RefreshShopData: player={inventory.Player.NetId}, items={inventory.CardEntries.Count() + inventory.RelicEntries.Count + inventory.PotionEntries.Count}");
            NotifyShopUpdated(inventory.Player.NetId);
        }
    }

    #region IPlayerPanelProvider Implementation

    public string Id => "shop";
    public int Priority => 30;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.shop").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return NMerchantRoom.Instance?.Room?.Inventories.Any(inv => inv.Player.NetId == player.NetId) == true;
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

        var inventory =
            NMerchantRoom.Instance?.Room.Inventories.FirstOrDefault(inv => inv.Player.NetId == player.NetId);
        if (inventory == null ||
            !(inventory.CardEntries.Any() || inventory.RelicEntries.Any() || inventory.PotionEntries.Any()))
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

        // 卡牌：横向布局
        foreach (var entry in inventory.CardEntries)
            if (entry is { CreationResult.Card: { } card, IsStocked: true })
                AddCardRow(container, player, entry, card);

        // 遗物：网格布局
        var relics = inventory.RelicEntries.Where(e => e is { Model: not null, IsStocked: true }).ToList();
        if (relics.Count > 0)
            AddItemGrid(container, player, relics, AddRelicItem);

        // 药水：网格布局
        var potions = inventory.PotionEntries.Where(e => e is { Model: not null, IsStocked: true }).ToList();
        if (potions.Count > 0)
            AddItemGrid(container, player, potions, AddPotionItem);

        Log.Debug(
            $"Updated content for player {player.NetId}: {inventory.CardEntries.Count()} cards, {inventory.RelicEntries.Count} relics, {inventory.PotionEntries.Count} potions");
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);

        Log.Debug($"SubscribeEvents for player {player.NetId}");

        void OnShopUpdated(ulong netId)
        {
            if (netId == player.NetId) onUpdate();
        }

        ShopUpdated += OnShopUpdated;
        player.GoldChanged += onUpdate;

        return () =>
        {
            Log.Debug($"UnsubscribeEvents for player {player.NetId}");
            ShopUpdated -= OnShopUpdated;
            player.GoldChanged -= onUpdate;
        };
    }

    public Action SubscribeVisibilityEvents(Player player, Action onVisibilityChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onVisibilityChanged);

        void OnShopUpdated(ulong netId)
        {
            if (netId == player.NetId) onVisibilityChanged();
        }

        ShopUpdated += OnShopUpdated;
        return () => ShopUpdated -= OnShopUpdated;
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
    private static void AddCardRow(VBoxContainer container, Player player, MerchantCardEntry entry, CardModel card)
    {
        card = card.ToMutable();

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);

        var priceLabel = CreatePriceLabel(player, entry.Cost, entry.IsOnSale);
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
    private static void AddItemGrid<T>(VBoxContainer container, Player player,
        List<T> items, Action<Player, T, HBoxContainer> addItem)
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
    private static void AddRelicItem(Player player, MerchantRelicEntry entry, HBoxContainer row)
    {
        var relic = entry.Model;
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
        var priceLabel = CreatePriceLabel(player, entry.Cost, false, true);
        container.AddChild(priceLabel);

        holder.GuiInput += @event => OnRelicGuiInput(holder, player, relic, @event);

        row.AddChild(container);
    }

    /// <summary>
    ///     添加药水项到行：物品在上，价格在下
    /// </summary>
    private static void AddPotionItem(Player player, MerchantPotionEntry entry, HBoxContainer row)
    {
        var potion = entry.Model;
        if (potion == null) return;

        var nPotion = NPotion.Create(potion.ToMutable());
        if (nPotion == null) return;

        var holder = NPotionHolder.Create(false);

        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
        };
        container.AddThemeConstantOverride("separation", 2);

        // 先加入场景树
        row.AddChild(container);
        container.AddChild(holder);
        holder.AddPotion(nPotion);
        nPotion.Position = Vector2.Zero;
        // 价格在下
        var priceLabel = CreatePriceLabel(player, entry.Cost, false, true);
        container.AddChild(priceLabel);

        holder.GuiInput += @event => OnPotionGuiInput(holder, player, potion, @event);
    }

    private static Label CreatePriceLabel(Player player, int cost, bool isOnSale = false, bool centered = false)
    {
        Label priceLabel;
        if (centered)
            priceLabel = new Label
            {
                Text = $"{cost}g",
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
        else
            priceLabel = new Label
            {
                Text = $"{cost}g",
                CustomMinimumSize = new Vector2(36, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        if (player.Gold < cost)
            priceLabel.AddThemeColorOverride("font_color", StsColors.red);
        else if (isOnSale)
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
