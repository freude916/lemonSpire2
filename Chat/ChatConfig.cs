using Godot;

namespace lemonSpire2.Chat;

/// <summary>
///     Configuration constants for the chat system.
/// </summary>
public static class ChatConfig
{
    // ========== Input ==========
    public const Key ToggleKey = Key.Tab;

    // ========== Layout ==========
    public const int CollapsedVisibleLines = 4;
    public const int FontSize = 18;

    // Position
    public const float PositionOffsetX = 5f;
    public const float PositionOffsetY = 5f;

    // Size percentages (relative to viewport)
    public const float PanelWidthRatio = 0.3f;
    public const float PanelHeightRatio = 0.5f;

    // Border
    public const int BorderWidth = 1;

    // ========== History ==========
    public const int MaxHistoryCount = 40;

    // ========== Fade Behavior ==========
    public const float FadeOutDelaySeconds = 10f;
    public const float FadeOutDurationSeconds = 2f;
    public const float FadeMinAlpha = 0.01f;
    public const float PanelBgAlpha = 0.80f;

    // ========== Colors ==========
    // Panel
    public static readonly Color PanelBgColor = new(0f, 0f, 0f, PanelBgAlpha);
    public static readonly Color PanelBorderColor = new(0.2f, 0.2f, 0.2f);

    // Input field
    public static readonly Color InputBgColor = new(0.1f, 0.1f, 0.1f, 0.9f);
    public static readonly Color PlaceholderColor = new(0.5f, 0.5f, 0.5f);
    public static readonly Color CaretColor = new(0f, 0.831f, 1f);

    // Message
    public static readonly Color TimeColor = new(0.53f, 0.53f, 0.53f); // #888888
    public static readonly Color SenderColor = new(0.8f, 0.8f, 1f); // #ccccff

    // ========== Helper Methods ==========

    /// <summary>
    ///     Get faded panel background color.
    /// </summary>
    public static Color GetFadedPanelBg(float alpha)
    {
        return new Color(0f, 0f, 0f, PanelBgAlpha * alpha);
    }

    /// <summary>
    ///     Get faded modulate color.
    /// </summary>
    public static Color GetFadedModulate(float alpha)
    {
        return new Color(1f, 1f, 1f, alpha);
    }
}
