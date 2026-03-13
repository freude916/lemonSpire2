using Godot;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Tooltips;

namespace lemonSpire2.Chat.Ui;

/// <summary>
///     Manages tooltip preview display using custom CreatePreview.
///     Positions tooltip with left-center alignment relative to mouse cursor.
/// </summary>
public sealed class TooltipManager
{
    private string? _currentMeta;
    private Control? _currentPreview;
    private Control? _parent;

    /// <summary>
    ///     是否有活动的 tooltip preview
    /// </summary>
    public bool HasPreview => _currentPreview is not null;

    public void RegisterHandlers(IntentHandlerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Register<IntentMetaHoverStart>(OnHoverStart);
        registry.Register<IntentMetaHoverEnd>(OnHoverEnd);
        registry.Register<IntentMetaClick>(OnClick);
    }

    public void Initialize(Control parent)
    {
        _parent = parent;
    }

    public void UpdatePreviewPosition(Vector2 globalMousePosition)
    {
        if (_currentPreview is null || _parent is null) return;

        var viewport = _parent.GetViewportRect().Size;
        _currentPreview.ResetSize();

        var tipWidth = _currentPreview.Size.X;
        var tipHeight = _currentPreview.Size.Y;

        // Left-center alignment
        var tipX = globalMousePosition.X + 16;
        var tipY = globalMousePosition.Y - tipHeight / 2;

        // Clamp Y
        if (tipY < 0) tipY = 0;
        else if (tipY + tipHeight > viewport.Y)
            tipY = viewport.Y - tipHeight;

        // Move to left of cursor if overflowing right edge
        if (tipX + tipWidth > viewport.X) tipX = globalMousePosition.X - tipWidth - 8;

        _currentPreview.GlobalPosition = new Vector2(tipX, tipY);
    }

    private void OnHoverStart(IntentMetaHoverStart intent)
    {
        if (_parent is null) return;

        var mousePosition = intent.GlobalPosition;

        // Skip if same meta
        if (_currentMeta == intent.Meta && _currentPreview is not null)
            return;

        ClearPreview();

        var tooltip = Tooltip.FromMetaString(intent.Meta);
        if (tooltip is null)
        {
            MainFile.Logger.Warn($"Failed to resolve tooltip from meta: {intent.Meta}");
            return;
        }

        var preview = tooltip.CreatePreview();
        if (preview is null)
        {
            MainFile.Logger.Warn($"CreatePreview returned null for {tooltip.GetType().Name}");
            return;
        }

        _currentPreview = preview;
        _currentMeta = intent.Meta;

        UpdatePreviewPosition(mousePosition);
        _parent.AddChild(preview);
    }

    private void OnHoverEnd(IntentMetaHoverEnd intent)
    {
        ClearPreview();
    }

    private void OnClick(IntentMetaClick intent)
    {
        ClearPreview();
    }

    private void ClearPreview()
    {
        if (_currentPreview is not null)
        {
            _currentPreview.QueueFree();
            _currentPreview = null;
        }

        _currentMeta = null;
    }
}
