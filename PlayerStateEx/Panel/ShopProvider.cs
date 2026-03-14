using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.PlayerStateEx.Shop;
using lemonSpire2.QoL;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace lemonSpire2.PlayerStateEx.Panel;

/// <summary>
///     商店显示提供者
///     显示玩家商店中的卡牌、遗物、药水
///     使用原生组件样式：NDeckHistoryEntry, NRelic, NPotion
///     支持 Alt+Click 发送物品到聊天
/// </summary>
public class ShopProvider : IPlayerPanelProvider
{
    private const float ItemScale = 0.5f;

    public string ProviderId => "shop";
    public int Priority => 30;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.shop").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return Shop.ShopManager.Instance.HasInventory(player.NetId);
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

        var items = Shop.ShopManager.Instance.GetInventory(player.NetId);
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
        var cards = items.Where(i => i.Type == Shop.ShopItemType.Card && i.IsStocked).ToList();
        var relics = items.Where(i => i.Type == Shop.ShopItemType.Relic && i.IsStocked).ToList();
        var potions = items.Where(i => i.Type == Shop.ShopItemType.Potion && i.IsStocked).ToList();

        // 卡牌行（逐行显示）
        foreach (var card in cards)
        {
            var row = CreateItemRow(player, card, () => CreateCardEntry(player, card, 1));
            if (row != null) container.AddChild(row);
        }

        // 遗物行
        foreach (var relic in relics)
        {
            var row = CreateItemRow(player, relic, () => CreateRelicControl(player, relic));
            if (row != null) container.AddChild(row);
        }

        // 药水行
        foreach (var potion in potions)
        {
            var row = CreateItemRow(player, potion, () => CreatePotionControl(player, potion));
            if (row != null) container.AddChild(row);
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

        // 金币图标
        var goldIcon = new TextureRect
        {
            Texture = GD.Load<Texture2D>(GoldIconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(16, 16),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        row.AddChild(goldIcon);

        // 金币数量
        var goldLabel = new Label
        {
            Text = player.Gold.ToString(),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        goldLabel.AddThemeColorOverride("font_color", StsColors.gold);
        goldLabel.AddThemeFontSizeOverride("font_size", 12);
        row.AddChild(goldLabel);

        // "Gold" 标签
        var titleLabel = new Label
        {
            Text = new LocString("gameplay_ui", "LEMONSPIRE.shop.gold").GetFormattedText(),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        titleLabel.AddThemeFontSizeOverride("font_size", 11);
        row.AddChild(titleLabel);

        return row;
    }

    private static HBoxContainer? CreateItemRow(Player player, Shop.ShopItemEntry entry, Func<Control?> createItem)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 4);

        // 价格标签
        var priceLabel = new Label
        {
            Text = $"{entry.Cost}g",
            CustomMinimumSize = new Vector2(36, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        // 价格颜色
        if (player.Gold < entry.Cost)
            priceLabel.AddThemeColorOverride("font_color", StsColors.red);
        else if (entry.IsOnSale)
            priceLabel.AddThemeColorOverride("font_color", StsColors.green);
        else
            priceLabel.AddThemeColorOverride("font_color", StsColors.cream);

        priceLabel.AddThemeFontSizeOverride("font_size", 11);
        row.AddChild(priceLabel);

        // 物品
        var item = createItem();
        if (item == null)
        {
            row.QueueFree();
            return null;
        }
        row.AddChild(item);

        return row;
    }

    public Action? SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);

        Shop.ShopManager.Instance.InventoryUpdated += OnInventoryUpdated;
        player.GoldChanged += OnGoldChanged;

        return () =>
        {
            Shop.ShopManager.Instance.InventoryUpdated -= OnInventoryUpdated;
            player.GoldChanged -= OnGoldChanged;
        };

        // 订阅金币变化
        void OnGoldChanged()
        {
            onUpdate();
        }

        // 订阅商店数据更新事件
        void OnInventoryUpdated(ulong netId)
        {
            if (netId == player.NetId) onUpdate();
        }
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ProviderUtils.ClearChildren(content);
    }

    private static NDeckHistoryEntry? CreateCardEntry(Player player, Shop.ShopItemEntry entry, int count)
    {
        var card = StsUtil.ResolveModel<CardModel>(entry.ModelId);

        if (card == null)
        {
            MainFile.Logger.Warn("[ShopProvider] Failed to resolve card model for ID: " + entry.ModelId);
            return null;
        }

        // 应用升级等级
        if (entry.UpgradeLevel > 0 && card.CurrentUpgradeLevel < entry.UpgradeLevel)
            card = card.ToMutable();

        var nEntry = NDeckHistoryEntry.Create(card, count);

        // 订阅点击事件
        nEntry.Connect(NDeckHistoryEntry.SignalName.Clicked,
            Callable.From<NDeckHistoryEntry>(_ => OnCardClicked(player, entry, card)));

        // 添加悬浮提示
        CardHoverTipHelper.BindCardHoverTip(nEntry, () => card, HoverTipAlignment.Right);

        return nEntry;
    }

    private static NRelic? CreateRelicControl(Player player, Shop.ShopItemEntry entry)
    {
        var relic = StsUtil.ResolveModel<RelicModel>(entry.ModelId);
        if (relic == null) return null;

        var nRelic = NRelic.Create(relic.ToMutable(), NRelic.IconSize.Small);
        if (nRelic == null) return null;

        nRelic.Scale = Vector2.One * ItemScale;
        nRelic.MouseFilter = Control.MouseFilterEnum.Pass;

        // 添加点击处理
        nRelic.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(e => OnRelicGuiInput(e, player, entry, relic)));

        return nRelic;
    }

    private static PotionControlWrapper CreatePotionControl(Player player, ShopItemEntry entry)
    {
        var potion = StsUtil.ResolveModel<PotionModel>(entry.ModelId);
        if (potion == null) return null;

        var nPotion = NPotion.Create(potion.ToMutable());
        if (nPotion == null) return null;

        var holder = NPotionHolder.Create(false);

        // 添加点击处理
        holder.Connect(NClickableControl.SignalName.Released,
            Callable.From(() => OnPotionClicked(player, entry, potion)));

        // NPotionHolder.AddPotion 需要 holder 在场景树中（_Ready 会初始化 _emptyIcon）
        // 使用包装容器，在 _Ready 中延迟初始化
        const float potionScale = 0.35f;
        var wrapper = new PotionControlWrapper(holder, nPotion, potionScale);
        return wrapper;
    }

    private static void OnCardClicked(Player player, Shop.ShopItemEntry entry, CardModel card)
    {
        if (!ProviderUtils.IsAltClick()) return;
        var segment = new TooltipSegment
        {
            Tooltip = CardTooltip.FromModel(card),
            DisplayName = $"{card.Title} ({entry.Cost}g{(entry.IsOnSale ? " Sale!" : "")})"
        };
        ProviderUtils.SendToChat(segment);
        MainFile.Logger.Debug($"[ShopProvider] Sent card to chat: {card.Title}");
    }

    private static void OnRelicGuiInput(InputEvent e, Player player, Shop.ShopItemEntry entry, RelicModel relic)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && ProviderUtils.IsAltClick())
        {
            var segment = new TooltipSegment
            {
                Tooltip = RelicTooltip.FromModel(relic),
                DisplayName = $"{relic.Title.GetFormattedText()} ({entry.Cost}g)"
            };
            ProviderUtils.SendToChat(segment);
            MainFile.Logger.Debug($"[ShopProvider] Sent relic to chat: {relic.Id.Entry}");
        }
    }

    private static void OnPotionClicked(Player player, Shop.ShopItemEntry entry, PotionModel potion)
    {
        if (ProviderUtils.IsAltClick())
        {
            var segment = new TooltipSegment
            {
                Tooltip = PotionTooltip.FromModel(potion),
                DisplayName = $"{potion.Title.GetFormattedText()} ({entry.Cost}g)"
            };
            ProviderUtils.SendToChat(segment);
            MainFile.Logger.Debug($"[ShopProvider] Sent potion to chat: {potion.Id.Entry}");
        }
    }
}
