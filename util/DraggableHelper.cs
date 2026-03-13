using Godot;

namespace lemonSpire2.util;

/// <summary>
///     可拖拽标题栏组件 - PanelContainer 实现
///     作为通用标题栏使用，内置拖拽功能、标题和可选关闭按钮
///     支持背景样式和 padding
/// </summary>
public partial class DraggableHeader : PanelContainer
{
    private readonly Button _closeButton = new()
    {
        Text = "X",
        CustomMinimumSize = new Vector2(20, 20),
        MouseFilter = MouseFilterEnum.Stop,
        Visible = false
    };

    private readonly HBoxContainer _hbox = new()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill
    };

    private readonly Label _titleLabel = new()
    {
        Text = "",
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        MouseFilter = MouseFilterEnum.Pass
    };

    private Control? _dragTarget;
    private bool _isDragging;
    private Vector2 _lastMousePos;
    private Action? _onClosePressed;
    private Action? _onDragEnd;
    private Action? _onDragStart;

    /// <summary>
    ///     是否启用拖拽
    /// </summary>
    public bool DragEnabled { get; set; } = true;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // 内部 HBoxContainer 布局
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        _hbox.AddChild(_titleLabel);

        _closeButton.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _closeButton.AddThemeColorOverride("font_hover_color", new Color(1f, 0.3f, 0.3f));
        _hbox.AddChild(_closeButton);

        AddChild(_hbox);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!DragEnabled || _dragTarget == null) return;

        if (@event is not InputEventMouseButton mb) return;
        if (mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            StartDrag(GetGlobalMousePosition());
            AcceptEvent();
        }
        else if (!mb.Pressed && mb.ButtonIndex == MouseButton.Left && _isDragging)
        {
            EndDrag();
        }
    }

    public override void _Process(double delta)
    {
        if (!_isDragging || _dragTarget == null) return;

        var mousePos = GetGlobalMousePosition();
        var deltaMove = mousePos - _lastMousePos;
        _lastMousePos = mousePos;

        _dragTarget.Position += deltaMove;
    }

    private void StartDrag(Vector2 mousePos)
    {
        _isDragging = true;
        _lastMousePos = mousePos;
        _onDragStart?.Invoke();
        _dragTarget!.MoveToFront();
    }

    private void EndDrag()
    {
        _isDragging = false;
        _onDragEnd?.Invoke();
    }

    /// <summary>
    ///     设置拖拽目标（默认为父控件）
    /// </summary>
    public DraggableHeader SetDragTarget(Control target)
    {
        _dragTarget = target;
        return this;
    }

    /// <summary>
    ///     设置拖拽回调
    /// </summary>
    public DraggableHeader SetDragCallbacks(Action? onDragStart = null, Action? onDragEnd = null)
    {
        _onDragStart = onDragStart;
        _onDragEnd = onDragEnd;
        return this;
    }

    /// <summary>
    ///     设置标题文本
    /// </summary>
    public DraggableHeader SetTitle(string text, int fontSize = 14, Color? color = null)
    {
        _titleLabel.Text = text;
        _titleLabel.AddThemeFontSizeOverride("font_size", fontSize);
        if (color.HasValue)
            _titleLabel.AddThemeColorOverride("font_color", color.Value);
        return this;
    }

    /// <summary>
    ///     获取标题 Label
    /// </summary>
    public Label GetTitleLabel() => _titleLabel;

    /// <summary>
    ///     显示关闭按钮并绑定回调
    /// </summary>
    public DraggableHeader ShowCloseButton(Action? onPressed = null)
    {
        _closeButton.Visible = true;
        if (onPressed != null)
        {
            if (_onClosePressed != null)
                _closeButton.Pressed -= _onClosePressed;
            _onClosePressed = onPressed;
            _closeButton.Pressed += onPressed;
        }
        return this;
    }

    /// <summary>
    ///     隐藏关闭按钮
    /// </summary>
    public DraggableHeader HideCloseButton()
    {
        _closeButton.Visible = false;
        return this;
    }

    /// <summary>
    ///     获取关闭按钮
    /// </summary>
    public Button GetCloseButton() => _closeButton;

    /// <summary>
    ///     完成初始化，自动设置父控件为拖拽目标
    /// </summary>
    public DraggableHeader Build()
    {
        _dragTarget ??= GetParent<Control>();
        return this;
    }
}