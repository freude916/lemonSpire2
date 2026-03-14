using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.PlayerStateEx.ShopEx;
using lemonSpire2.QoL;
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

namespace lemonSpire2.PlayerStateEx.OverlayPanel;

/// <summary>
///     商店显示提供者
///     显示玩家商店中的卡牌、遗物、药水
///     使用原生组件样式：NDeckHistoryEntry, NRelic, NPotion
///     支持 Alt+Click 发送物品到聊天
/// </summary>
public class ShopProvider : IPlayerPanelProvider
{
    private const float ItemScale = 0.5f;
    private const float PotionScale = 0.6f;

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
        container.AddThemeConstantOverride("separation", 4);

        return container;
    }

    private const string GoldIconPath = "res://images/packed/sprite_fonts/gold_icon.png";

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (content is not VBoxContainer container) return;

        // 清除现有内容
        ProviderUtils.ClearChildren(container);

        // 显示对方金币
        var goldRow = CreateGoldRow(player);
        container.AddChild(goldRow);

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

        // 分组显示：卡牌、遗物、药水
        var cards = items.Where(i => i is { Type: ShopItemType.Card, IsStocked: true }).ToList();
        var relics = items.Where(i => i is { Type: ShopItemType.Relic, IsStocked: true }).ToList();
        var potions = items.Where(i => i is { Type: ShopItemType.Potion, IsStocked: true }).ToList();

        // 卡牌行
        foreach (var card in cards)
        {
            AddCardRow(container, player, card);
        }

        // 遗物行
        foreach (var relic in relics)
        {
            AddRelicRow(container, player, relic);
        }

        // 药水行
        foreach (var potion in potions)
        {
            AddPotionRow(container, player, potion);
        }

        MainFile.Logger.Debug(
            $"[ShopProvider] Updated content for player {player.NetId}: {cards.Count} cards, {relics.Count} relics, {potions.Count} potions");
    }

    private static HBoxContainer CreateGoldRow(Player player)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
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
            Text = player.Gold.ToString(),
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

    private static void AddCardRow(VBoxContainer container, Player player, ShopItemEntry entry)
    {
        var card = StsUtil.ResolveModel<CardModel>(entry.ModelId);
        if (card == null) return;

        if (entry.UpgradeLevel > 0 && card.CurrentUpgradeLevel < entry.UpgradeLevel)
            card = card.ToMutable();

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);

        AddPriceLabel(row, player, entry);

        var nEntry = NDeckHistoryEntry.Create(card, 1);
        nEntry.Connect(NDeckHistoryEntry.SignalName.Clicked,
            Callable.From<NDeckHistoryEntry>(_ => OnCardClicked(player, entry, card)));
        CardHoverTipHelper.BindCardHoverTip(nEntry, () => card, HoverTipAlignment.Right);
        row.AddChild(nEntry);

        container.AddChild(row);
    }

    private static void AddRelicRow(VBoxContainer container, Player player, ShopItemEntry entry)
    {
        var relic = StsUtil.ResolveModel<RelicModel>(entry.ModelId);
        if (relic == null) return;

        var holder = NRelicBasicHolder.Create(relic.ToMutable());
        if (holder == null) return;

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);

        AddPriceLabel(row, player, entry);
        row.AddChild(holder);
        container.AddChild(row);

        // 现在 row 在场景树中，holder._Ready() 已执行
        holder.Connect(NClickableControl.SignalName.Released,
            Callable.From(() => OnRelicClicked(player, entry, relic)));

        // 设置缩放（与 PotionProvider 一样的模式）
        var nRelic = holder.Relic;
        if (nRelic != null)
        {
            nRelic.PivotOffset = nRelic.Size * 0.5f;  // 关键：设置中心点。见 Sts2...NPotionHolder.AddPotion
            nRelic.Position = Vector2.Zero; // 关键：重置位置，否则会出现偏移
            nRelic.Scale = Vector2.One * ItemScale;
            holder.CustomMinimumSize = nRelic.Size * ItemScale;
        }
    }

    private static void AddPotionRow(VBoxContainer container, Player player, ShopItemEntry entry)
    {
        var potion = StsUtil.ResolveModel<PotionModel>(entry.ModelId);
        if (potion == null) return;

        var nPotion = NPotion.Create(potion.ToMutable());
        if (nPotion == null) return;

        var holder = NPotionHolder.Create(false);

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);

        AddPriceLabel(row, player, entry);
        row.AddChild(holder);
        container.AddChild(row);

        // 现在 row 在场景树中，holder._Ready() 已执行
        holder.Connect(NClickableControl.SignalName.Released,
            Callable.From(() => OnPotionClicked(player, entry, potion)));

        // 与 PotionProvider 完全一样的模式
        holder.AddPotion(nPotion);
        nPotion.Position = Vector2.Zero;
        ProviderUtils.SetPotionScale(holder, PotionScale);
        nPotion.Scale = Vector2.One * PotionScale;
        holder.CustomMinimumSize = nPotion.Size * PotionScale;
    }

    private static void AddPriceLabel(HBoxContainer row, Player player, ShopItemEntry entry)
    {
        var priceLabel = new Label
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

        priceLabel.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(priceLabel);
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);

        MainFile.Logger.Debug($"[ShopProvider] SubscribeEvents for player {player.NetId}");

        ShopManager.Instance.InventoryUpdated += OnInventoryUpdated;
        player.GoldChanged += OnGoldChanged;

        return () =>
        {
            MainFile.Logger.Debug($"[ShopProvider] UnsubscribeEvents for player {player.NetId}");
            ShopManager.Instance.InventoryUpdated -= OnInventoryUpdated;
            player.GoldChanged -= OnGoldChanged;
        };

        void OnGoldChanged()
        {
            MainFile.Logger.Debug($"[ShopProvider] OnGoldChanged for player {player.NetId}");
            onUpdate();
        }

        void OnInventoryUpdated(ulong netId)
        {
            MainFile.Logger.Debug($"[ShopProvider] OnInventoryUpdated: netId={netId}, player.NetId={player.NetId}");
            if (netId == player.NetId) onUpdate();
        }
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ProviderUtils.ClearChildren(content);
    }

    private static void OnCardClicked(Player player, ShopItemEntry entry, CardModel card)
    {
        if (!ProviderUtils.IsAltClick()) return;
        var segment = new TooltipSegment { Tooltip = CardTooltip.FromModel(card) };
        ProviderUtils.SendToChat(segment);
        MainFile.Logger.Debug($"[ShopProvider] Sent card to chat: {card.Title}");
    }

    private static void OnRelicClicked(Player player, ShopItemEntry entry, RelicModel relic)
    {
        if (!ProviderUtils.IsAltClick()) return;
        var segment = new TooltipSegment { Tooltip = RelicTooltip.FromModel(relic) };
        ProviderUtils.SendToChat(segment);
        MainFile.Logger.Debug($"[ShopProvider] Sent relic to chat: {relic.Id.Entry}");
    }

    private static void OnPotionClicked(Player player, ShopItemEntry entry, PotionModel potion)
    {
        if (!ProviderUtils.IsAltClick()) return;
        var segment = new TooltipSegment { Tooltip = PotionTooltip.FromModel(potion) };
        ProviderUtils.SendToChat(segment);
        MainFile.Logger.Debug($"[ShopProvider] Sent potion to chat: {potion.Id.Entry}");
    }
}
