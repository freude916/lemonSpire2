using Godot;
using lemonSpire2.Chat.Models;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace lemonSpire2.Chat.UI;

/// <summary>
///     Hover 预览管理器 — 管理物品链接的悬浮预览
/// </summary>
public sealed class HoverPreviewManager : IDisposable
{
    private string? _activeMeta;
    private Control? _activePreview;
    private Control? _root;

    public void Dispose() => DismissPreview();

    /// <summary>
    ///     初始化管理器（设置根节点）
    /// </summary>
    public void Initialize(Control root) => _root = root;

    /// <summary>
    ///     处理 meta hover 开始
    /// </summary>
    public void OnMetaHoverStarted(string meta)
    {
        if (_root == null)
        {
            return;
        }

        // 相同 meta 不重复创建
        if (meta == _activeMeta)
        {
            return;
        }

        // 清除旧预览
        DismissPreview();

        // 尝试解析 meta
        if (!ItemLink.TryParseMeta(meta, out var segment))
        {
            MainFile.Logger.Info($"[HoverPreviewManager] Failed to parse meta: {meta}");
            return;
        }

        MainFile.Logger.Info($"[HoverPreviewManager] Parsed segment: {segment.LinkType}, {segment.DisplayName}");

        // 卡牌需要特殊处理：先 AddChild，再初始化
        if (segment.LinkType == ItemLinkType.Card)
        {
            var preview = CreateCardContainer();
            if (preview == null)
            {
                MainFile.Logger.Info($"[HoverPreviewManager] Failed to create card container");
                return;
            }

            preview.MouseFilter = Control.MouseFilterEnum.Ignore;
            SetSubtreeMouseIgnore(preview);
            _root.AddChild(preview);

            // 关键：在 AddChild 之后再初始化卡牌
            if (!InitCardPreview(preview, segment))
            {
                preview.QueueFree();
                MainFile.Logger.Info($"[HoverPreviewManager] Failed to init card preview");
                return;
            }

            _activePreview = preview;
            _activeMeta = meta;
            UpdatePreviewPosition();
            return;
        }

        // 其他物品类型的处理
        var otherPreview = CreatePreview(segment);
        if (otherPreview == null)
        {
            MainFile.Logger.Info($"[HoverPreviewManager] Failed to create preview");
            return;
        }

        MainFile.Logger.Info($"[HoverPreviewManager] Preview created successfully");

        otherPreview.MouseFilter = Control.MouseFilterEnum.Ignore;
        SetSubtreeMouseIgnore(otherPreview);
        otherPreview.AnchorsPreset = (int)Control.LayoutPreset.TopLeft;

        _root.AddChild(otherPreview);
        _activePreview = otherPreview;
        _activeMeta = meta;
        UpdatePreviewPosition();
    }

    /// <summary>
    ///     处理 meta hover 结束
    /// </summary>
    public void OnMetaHoverEnded(string meta)
    {
        if (_activeMeta == meta)
        {
            DismissPreview();
        }
    }

    /// <summary>
    ///     更新预览位置（跟随鼠标）
    /// </summary>
    public void UpdatePreviewPosition()
    {
        if (_activePreview == null || _root == null)
        {
            return;
        }

        if (!GodotObject.IsInstanceValid(_activePreview))
        {
            _activePreview = null;
            _activeMeta = null;
            return;
        }

        var viewport = _root.GetViewport();
        if (viewport == null)
        {
            return;
        }

        var mousePos = viewport.GetMousePosition();
        var vpSize = viewport.GetVisibleRect().Size;

        _activePreview.ResetSize();
        var pw = _activePreview.Size.X;
        var ph = _activePreview.Size.Y;

        // 将预览的左中心锚点放在鼠标位置右侧一点
        var x = mousePos.X + 12f;
        var y = mousePos.Y - ph * 0.5f;

        // 如果右侧空间不够，放左侧
        if (x + pw > vpSize.X - 4f)
        {
            x = mousePos.X - pw - 12f;
        }

        // 垂直边界检查
        y = Mathf.Clamp(y, 4f, vpSize.Y - ph - 4f);

        _activePreview.GlobalPosition = new Vector2(x, y);
    }

    /// <summary>
    ///     清除当前预览
    /// </summary>
    public void DismissPreview()
    {
        if (_activePreview != null && GodotObject.IsInstanceValid(_activePreview))
        {
            _activePreview.QueueFree();
        }

        _activePreview = null;
        _activeMeta = null;
    }

    /// <summary>
    ///     根据物品类型创建预览控件（不包括卡牌）
    /// </summary>
    private static Control? CreatePreview(ItemSegment segment) =>
        segment.LinkType switch
        {
            ItemLinkType.Potion => CreatePotionPreview(segment),
            ItemLinkType.Relic => CreateRelicPreview(segment),
            ItemLinkType.Power => CreatePowerPreview(segment),
            ItemLinkType.Target => CreateTargetPreview(segment),
            _ => null
        };

    private static void SetSubtreeMouseIgnore(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control c)
            {
                c.MouseFilter = Control.MouseFilterEnum.Ignore;
            }

            SetSubtreeMouseIgnore(child);
        }
    }

    #region 预览创建

    /// <summary>
    ///     创建卡牌容器（不初始化）
    /// </summary>
    private static Control? CreateCardContainer()
    {
        try
        {
            return PreloadManager.Cache
                .GetScene("res://scenes/ui/card_hover_tip.tscn")
                .Instantiate<Control>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     初始化卡牌预览（需要在 AddChild 之后调用）
    /// </summary>
    private static bool InitCardPreview(Control container, ItemSegment segment)
    {
        var card = ItemLink.ResolveCard(segment.ModelId, segment.UpgradeLevel);
        if (card == null)
        {
            return false;
        }

        try
        {
            var nCard = container.GetNode<NCard>("%Card");
            nCard.Model = card;
            nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            container.ResetSize();
            return true;
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Error($"[InitCardPreview] Error: {ex.Message}");
            return false;
        }
    }

    private static Control? CreatePotionPreview(ItemSegment segment)
    {
        var potion = ItemLink.ResolvePotion(segment.ModelId);
        if (potion == null)
        {
            return null;
        }

        return CreateHoverTipControl(potion.HoverTip);
    }

    private static Control? CreateRelicPreview(ItemSegment segment)
    {
        var relic = ItemLink.ResolveRelic(segment.ModelId);
        if (relic == null)
        {
            return null;
        }

        return CreateHoverTipControl(relic.HoverTip);
    }

    private static Control? CreatePowerPreview(ItemSegment segment)
    {
        // 判断是否是玩家（非敌人）
        var isPlayer = segment.CreatureColorHex != "FF5555";
        var tip = ItemLink.CreatePowerHoverTip(segment, isPlayer);
        if (tip == null)
        {
            return null;
        }

        return CreateHoverTipControl(tip.Value);
    }

    private static PanelContainer? CreateTargetPreview(ItemSegment segment)
    {
        // Target 的预览：显示名称和颜色
        PanelContainer? panel = new();

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var label = new Label
        {
            Text = segment.CreatureName ?? segment.DisplayName
        };

        // 使用生物颜色
        if (!string.IsNullOrEmpty(segment.CreatureColorHex))
        {
            try
            {
                var color = Color.FromHtml($"#{segment.CreatureColorHex}");
                label.AddThemeColorOverride("font_color", color);
            }
            catch
            {
                label.AddThemeColorOverride("font_color", Colors.White);
            }
        }

        label.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(label);

        return panel;
    }

    private static Control? CreateHoverTipControl(HoverTip tip)
    {
        try
        {
            var scene = PreloadManager.Cache.GetScene("res://scenes/ui/hover_tip.tscn");
            var control = scene.Instantiate<Control>();

            var title = control.GetNode<MegaLabel>("%Title");
            if (tip.Title == null)
            {
                title.Visible = false;
            }
            else
            {
                title.SetTextAutoSize(tip.Title);
            }

            control.GetNode<MegaRichTextLabel>("%Description").Text = tip.Description;
            control.GetNode<TextureRect>("%Icon").Texture = tip.Icon;

            if (tip.IsDebuff)
            {
                var bg = control.GetNode<CanvasItem>("%Bg");
                bg.Material = PreloadManager.Cache.GetMaterial("res://materials/ui/hover_tip_debuff.tres");
            }

            control.ResetSize();
            return control;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
