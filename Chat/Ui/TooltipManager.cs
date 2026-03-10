using Godot;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Tooltips;

namespace lemonSpire2.Chat.Ui;

/// <summary>
///     Manages tooltip preview display based on Intent dispatch.
///     Subscribes to IntentMetaHoverStart/End via the IntentHandlerRegistry.
/// </summary>
public sealed class TooltipManager : IDisposable
{
    private string? _currentMeta;
    private Control? _currentPreview;
    private bool _disposed;
    private Control? _parent;

    public void Dispose()
    {
        _currentPreview?.Dispose();
        if (_disposed) return;
        _disposed = true;
        ClearPreview();
        _parent = null;
    }

    /// <summary>
    ///     Registers tooltip intent handlers with the given registry.
    /// </summary>
    public void RegisterHandlers(IntentHandlerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Register<IntentMetaHoverStart>(OnHoverStart);
        registry.Register<IntentMetaHoverEnd>(OnHoverEnd);
        registry.Register<IntentMetaClick>(OnClick);
    }

    /// <summary>
    ///     Sets the parent control for preview popups.
    ///     Must be called before any hover events.
    /// </summary>
    public void Initialize(Control parent)
    {
        _parent = parent;
    }

    /// <summary>
    ///     Updates preview position to follow mouse. Called from _Process.
    /// </summary>
    public void UpdatePreviewPosition(Vector2 globalMousePosition)
    {
        _currentPreview.Position = globalMousePosition + new Vector2(16, 16);
    }

    private void OnHoverStart(IntentMetaHoverStart intent)
    {
        MainFile.Logger.Debug($"OnHoverStart: meta={intent.Meta}, parent={_parent?.Name ?? "null"}");
        
        if (_disposed || _parent is null) return;

        var mousePosition = intent.GlobalPosition;

        // Skip if same meta
        if (_currentMeta == intent.Meta && _currentPreview is not null)
            return;

        // Clear previous preview
        ClearPreview();

        // Try resolve tooltip from registry
        var tooltip = Tooltip.FromMetaString(intent.Meta);
        if (tooltip is null)
        {
            MainFile.Logger.Warn($"Failed to resolve tooltip from meta: {intent.Meta}");
            return;
        }

        MainFile.Logger.Debug($"Resolved tooltip: {tooltip.GetType().Name}, Id={tooltip.Id}");

        // Create preview
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
        MainFile.Logger.Info($"Tooltip preview added at {preview.Position}");
    }

    private void OnHoverEnd(IntentMetaHoverEnd intent)
    {
        if (_disposed) return;
        ClearPreview();
    }

    private void OnClick(IntentMetaClick intent)
    {
        if (_disposed) return;

        // Future: handle click action (e.g., inspect card, execute command)
        // For now, just clear preview
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