using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace lemonSpire2.util.Ui;

/// <summary>
///     全局窗口大小变化事件分发器
///     Panel 可以订阅 OnViewportResized 事件来响应窗口变化
/// </summary>
public sealed partial class ViewportResizeNotifier : Control
{
    private static ViewportResizeNotifier? _instance;

    private Vector2 _lastViewportSize;

    /// <summary>
    ///     单例实例
    /// </summary>
    public static ViewportResizeNotifier Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = new ViewportResizeNotifier();
            // 添加到场景树以便接收 _Process 回调
            NRun.Instance?.GlobalUi.AddChild(_instance);
            return _instance;
        }
    }

    /// <summary>
    ///     窗口大小变化事件，参数为新的大小
    /// </summary>
    public event Action<Vector2>? OnViewportResized;

    public override void _Ready()
    {
        Name = "ViewportResizeNotifier";
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;

        // 初始化记录
        _lastViewportSize = GetViewportRect().Size;
    }

    public override void _Process(double delta)
    {
        var currentSize = GetViewportRect().Size;
        if (currentSize == _lastViewportSize) return;
        _lastViewportSize = currentSize;
        OnViewportResized?.Invoke(currentSize);
    }

    /// <summary>
    ///     获取当前视口大小
    /// </summary>
    public Vector2 GetViewportSize()
    {
        return GetViewportRect().Size;
    }

    /// <summary>
    ///     清理单例（用于测试或重新初始化）
    /// </summary>
    public static void Cleanup()
    {
        if (_instance != null)
        {
            _instance.QueueFree();
            _instance = null;
        }
    }
}
