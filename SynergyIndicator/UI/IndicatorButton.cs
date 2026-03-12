using Godot;
using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.SynergyIndicator.Ui;

/// <summary>
/// 单个指示器按钮，支持 emoji 和图标两种显示方式，响应点击事件并触发状态切换
/// </summary>
public partial class IndicatorButton : Button
{
    public event Action<IndicatorType>? IndicatorClicked;
    public IndicatorType Type { get; private set; }

    private Label? EmojiLabel { get; set; }
    private TextureRect? IconTextureRect { get; set; }
    public IndicatorStatus Status { get; private set; }

    // emoji 映射
    private static readonly Dictionary<IndicatorType, string> Emojis = new()
    {
        { IndicatorType.HandShake, "🤝" }
    };

    public override void _Ready()
    {
        base._Ready();

        Text = "";
        CustomMinimumSize = new Vector2(32, 32);
    }

    public void Setup(IndicatorType type, IndicatorStatus status = IndicatorStatus.WillUse)
    {
        Type = type;
        Status = status;

        EmojiLabel?.QueueFree();
        IconTextureRect?.QueueFree();

        if (Emojis.TryGetValue(type, out var emoji))
        {
            SetupEmoji(emoji);
        }
        else
        {
            SetupIcon(type);
        }

        Pressed -= OnIndicatorClicked;
        Pressed += OnIndicatorClicked;

        // IndicatorHandler.SendMessage(type, status);
        UpdateStatusVisual();
        PlayFlashAnimation();
    }

    /// <summary>
    /// 更新指示器状态并刷新视觉效果
    /// </summary>
    public void SetStatus(IndicatorStatus status)
    {
        Status = status;
        // IndicatorHandler.SendMessage(Type, status);
        UpdateStatusVisual();
    }

    /// <summary>
    /// 根据当前状态更新视觉效果（WontUse 时半透明）
    /// </summary>
    private void UpdateStatusVisual()
    {
        Modulate = Status == IndicatorStatus.WontUse
            ? new Color(1, 1, 1, 0.3f) // 30% 不透明度
            : new Color(1, 1, 1); // 完全不透明
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EmojiLabel?.QueueFree();
            IconTextureRect?.QueueFree();
        }
        base.Dispose(disposing);
    }

    public void PlayFlashAnimation()
    {
        Control? target = (Control?)EmojiLabel ?? IconTextureRect;
        if (target == null) return;

        target.PivotOffset = target.Size / 2;
        var tween = CreateTween();
        tween.SetLoops(3);

        tween.TweenProperty(target, "scale", Vector2.One * 1.5f, 0.2).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(target, "modulate", Colors.Yellow, 0.2);

        tween.TweenProperty(target, "scale", Vector2.One, 0.2).SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(target, "modulate", Colors.White, 0.2);
    }

    private void SetupEmoji(string emoji)
    {
        EmojiLabel = new Label
        {
            Text = emoji,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // 设置 Label 填充整个按钮
        EmojiLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        EmojiLabel.MouseFilter = MouseFilterEnum.Ignore; // 让点击事件穿透到 Button

        AddChild(EmojiLabel);
    }

    private void SetupIcon(IndicatorType type)
    {
        var icon = GetIconForIndicatorType(type);
        if (icon == null) return;

        IconTextureRect = new TextureRect
        {
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };

        // 设置 TextureRect 填充整个按钮
        IconTextureRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        IconTextureRect.MouseFilter = MouseFilterEnum.Ignore;

        AddChild(IconTextureRect);
    }

    private void OnIndicatorClicked()
    {
        IndicatorClicked?.Invoke(Type);
    }

    private static Texture2D? GetIconForIndicatorType(IndicatorType type)
    {
        string? powerId = type switch
        {
            // TODO
            _ => null
        };

        if (powerId == null)
        {
            return null;
        }

        var resource = ModelDb.AllPowers.FirstOrDefault(p => p.Id.Entry == powerId);
        return resource?.Icon;
    }
}