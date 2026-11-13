using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     极速商店选项的缩小展示。复用原版视觉资源，点击只用于分享和提醒。
/// </summary>
public partial class BlitzShopEntryPreview : Control
{
    private const float PreviewSize = 80f;
    private const float NativeSize = 240f;
    private const float PreviewScale = PreviewSize / NativeSize;

    private Control _visual = null!;
    private Tween? _hoverTween;

    public BlitzMerchantEntry Entry { get; private set; } = null!;

    public static BlitzShopEntryPreview Create(BlitzMerchantEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new BlitzShopEntryPreview
        {
            Entry = entry,
            CustomMinimumSize = Vector2.One * PreviewSize,
            MouseFilter = entry.IsStocked ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore
        };
    }

    public override void _Ready()
    {
        _visual = new Control
        {
            Size = Vector2.One * NativeSize,
            Scale = Vector2.One * PreviewScale,
            PivotOffset = Vector2.One * NativeSize * 0.5f,
            Position = Vector2.One * (PreviewSize - NativeSize) * 0.5f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_visual);

        // 原版槽位始终不入树，仅转移视觉节点，避免绑定其购买操作。
        var template = PreloadManager.Cache
            .GetScene("res://scenes/merchant/blitz_merchant_entry.tscn")
            .Instantiate<Control>();
        try
        {
            var hitbox = template.GetNode<Control>("Hitbox");
            foreach (var child in hitbox.GetChildren())
            {
                hitbox.RemoveChild(child);
                _visual.AddChild(child);
            }
        }
        finally
        {
            template.Free();
        }

        var icon = _visual.GetNode<TextureRect>("Icon");
        icon.Texture = Entry.Icon;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        var shadow = icon.GetNode<TextureRect>("Shadow");
        shadow.Texture = icon.Texture;
        shadow.MouseFilter = MouseFilterEnum.Ignore;
        _visual.GetNode<GpuParticles2D>("RareVfx").Visible = Entry.IsRare;
        _visual.GetNode<GpuParticles2D>("CursedVfx").Visible = Entry.IsCursed;

        _visual.Visible = Entry.Status != BlitzMerchantEntry.BlitzEntryStatus.Used;
        _visual.Modulate = Entry.Status == BlitzMerchantEntry.BlitzEntryStatus.Disabled
            ? Colors.DarkGray
            : Colors.White;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public override void _ExitTree()
    {
        _hoverTween?.Kill();
        NHoverTipSet.Remove(this);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Entry.IsStocked || StsUtil.IsInSelection(this))
        {
            return;
        }

        if (@event is InputEventMouseButton
            { Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right } mouse)
        {
            PlayerPanelChatHelper.SendBlitzShopEntryToChat(Entry.Player, Entry,
                includeExtraHoverTips: mouse.ButtonIndex == MouseButton.Right);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            PlayerPanelChatHelper.RequestRemoteFlash(Entry.Player, Entry);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnMouseEntered()
    {
        if (!Entry.IsStocked || StsUtil.IsInSelection(this))
        {
            return;
        }

        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_visual, "scale", Vector2.One * PreviewScale * 1.1f, 0.05);
        var alignment = GetIndex() % 3 == 2 ? HoverTipAlignment.Left : HoverTipAlignment.Right;
        NHoverTipSet.CreateAndShow(this, Entry.HoverTips, alignment)?.SetFollowOwner();
    }

    private void OnMouseExited()
    {
        NHoverTipSet.Remove(this);
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_visual, "scale", Vector2.One * PreviewScale, 0.5)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
    }
}
