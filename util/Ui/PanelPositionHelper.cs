using Godot;

namespace lemonSpire2.util.Ui;

/// <summary>
///     面板位置工具类
///     提供 Clamp 到视口内的功能
/// </summary>
public static class PanelPositionHelper
{
    /// <summary>
    ///     将面板位置 Clamp 到视口内（X 和 Y 方向都限制）
    /// </summary>
    /// <param name="panel">要 Clamp 的面板</param>
    /// <param name="margin">边缘留白</param>
    /// <param name="clampX">Do X Clamp</param>
    /// <param name="clampY">Do Y Clamp</param>
    public static void ClampToViewport(Control? panel, float margin = 10f, bool clampX = true, bool clampY = true)
    {
        if (panel == null || !panel.IsInsideTree()) return;

        var viewportSize = panel.GetViewportRect().Size;
        var panelSize = panel.Size;

        var x = panel.GlobalPosition.X;
        var y = panel.GlobalPosition.Y;

        // X 方向 Clamp
        var minX = margin;
        var maxX = viewportSize.X - panelSize.X - margin;
        if (clampX)
            x = maxX < minX
                ? minX
                : Mathf.Clamp(x, minX,
                    maxX); // Control too wide... but close button is at right ?? how to solve this...

        // Y 方向 Clamp
        var minY = margin;
        var maxY = viewportSize.Y - panelSize.Y - margin;
        if (clampY)
            y = maxY < minY
                ? minY
                : Mathf.Clamp(y, minY,
                    maxY); // Control too high! since title bar is at top for drag, we have to keep that

        panel.GlobalPosition = new Vector2(x, y);
    }
}
