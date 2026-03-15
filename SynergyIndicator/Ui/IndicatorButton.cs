using Godot;
using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace lemonSpire2.SynergyIndicator.Ui;

/// <summary>
///     单个指示器按钮，支持 emoji 和图标两种显示方式，响应点击事件并触发状态切换
/// </summary>
public partial class IndicatorButton : Button
{
    private static readonly Dictionary<IndicatorType, Texture2D?> IconCache = new()
    {
        { IndicatorType.Vulnerable, PowerIcon<VulnerablePower>() },
        { IndicatorType.Weak, PowerIcon<WeakPower>() }
    };

    // emoji 映射
    private static readonly Dictionary<IndicatorType, string> Emojis = new()
    {
        { IndicatorType.HandShake, "🤝" }
    };

    private static readonly StyleBoxFlat InteractiveStyle = new()
    {
        BgColor = new Color(1, 1, 1, 0), // 透明背景
        BorderColor = new Color(1, 1, 1, 0), // 透明边框
        BorderWidthLeft = 2,
        BorderWidthRight = 2,
        BorderWidthTop = 2,
        BorderWidthBottom = 2,
        CornerRadiusTopLeft = 4,
        CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4,
        CornerRadiusBottomRight = 4
    };

    private bool _isInteractive;
    public IndicatorType Type { get; private set; }

    private Label? EmojiLabel { get; set; }
    private TextureRect? IconTextureRect { get; set; }
    public IndicatorStatus Status { get; private set; }
# pragma warning disable CA1003
    public event Action<IndicatorType>? IndicatorClicked;
# pragma warning restore CA1003

    public override void _Ready()
    {
        base._Ready();

        Text = "";
        CustomMinimumSize = new Vector2(32, 32);
    }

    public void Setup(IndicatorType type, IndicatorStatus status = IndicatorStatus.WillUse, bool isInteractive = false)
    {
        Type = type;
        Status = status;
        _isInteractive = isInteractive;

        EmojiLabel?.QueueFree();
        IconTextureRect?.QueueFree();

        if (Emojis.TryGetValue(type, out var emoji))
            SetupEmoji(emoji);
        else
            SetupIcon(type);

        Pressed += OnIndicatorClicked;

        // 可交互时添加边框样式，并覆盖 focus 样式去掉默认边框
        if (isInteractive)
        {
            AddThemeStyleboxOverride("normal", InteractiveStyle);
            AddThemeStyleboxOverride("focus", InteractiveStyle);
        }

        UpdateStatusVisual();
        PlayFlashAnimation();
    }

    /// <summary>
    ///     更新指示器状态并刷新视觉效果
    /// </summary>
    public void SetStatus(IndicatorStatus status)
    {
        if (Status == status) return;

        var previousStatus = Status;
        Status = status;
        UpdateStatusVisual();

        if (previousStatus == IndicatorStatus.WontUse && status == IndicatorStatus.WillUse)
            PlayFlashAnimation();
    }

    /// <summary>
    ///     根据当前状态更新视觉效果（WontUse 时半透明）
    /// </summary>
    private void UpdateStatusVisual()
    {
        Modulate = Status == IndicatorStatus.WontUse
            ? new Color(1, 1, 1, 0.3f) // 30% 不透明度
            : new Color(1, 1, 1); // 完全不透明
    }

    public void PlayFlashAnimation()
    {
        var target = (Control?)EmojiLabel ?? IconTextureRect;
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

        // 增大 emoji 字体大小以匹配图标
        EmojiLabel.AddThemeFontSizeOverride("font_size", 24);

        // 设置 Label 填充整个按钮
        EmojiLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

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

        AddChild(IconTextureRect);
    }

    private void OnIndicatorClicked()
    {
        IndicatorClicked?.Invoke(Type);
    }

    private static Texture2D? GetIconForIndicatorType(IndicatorType type)
    {
        return IconCache.GetValueOrDefault(type);
    }

    private static Texture2D? PowerIcon<T>() where T : PowerModel
    {
        return ModelDb.AllPowers.FirstOrDefault(p => p is T)?.Icon;
    }
}
