using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Chat;

/// <summary>
///     聊天UI组件 - 类似DevConsole风格，按Tab切换显示
/// </summary>
public partial class ChatUi : Panel
{
    private const Key ToggleKey = Key.Tab;
    private const Key SendKey = Key.Enter;

    private const int CollapsedVisibleLines = 2;
    private const int FontSize = 18;
    private const float InputHeight = 40f;
    private const float FadeOutDelaySeconds = 5f;
    private const float FadeOutDurationSeconds = 5f;

    private readonly List<ChatMessageEntry> _messages = [];
    private LineEdit _inputBuffer = null!;
    private Control _inputContainer = null!;
    private bool _isExpanded;
    private bool _isFadedOut;
    private bool _isInputFocused;
    private double _lastMessageTime;

    private RichTextLabel _outputBuffer = null!;
    private StyleBoxFlat _panelStyle = null!;
    private Label _promptLabel = null!;

    public event Action<string>? OnMessageSent;

    public override void _Ready()
    {
        // 设置为半屏宽度，左侧
        AnchorLeft = 0;
        AnchorTop = 1;
        AnchorRight = 0;
        AnchorBottom = 1;

        CreateUi();
        UpdateLayout();
    }

    private void CreateUi()
    {
        // 背景样式
        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.85f),
            BorderColor = new Color(0.2f, 0.2f, 0.2f),
            BorderWidthTop = 1,
            BorderWidthRight = 1
        };
        AddThemeStyleboxOverride("panel", _panelStyle);

        // 输出区域
        _outputBuffer = new RichTextLabel
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            BbcodeEnabled = true,
            ScrollActive = true,
            ScrollFollowing = true,
            MouseFilter = MouseFilterEnum.Stop
        };
        _outputBuffer.AddThemeColorOverride("default_color", Colors.White);
        _outputBuffer.AddThemeFontSizeOverride("normal_font_size", FontSize);
        AddChild(_outputBuffer);

        // 输入容器
        _inputContainer = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(_inputContainer);

        // 提示符
        _promptLabel = new Label
        {
            Text = "➜",
            CustomMinimumSize = new Vector2(30, 0)
        };
        _promptLabel.AddThemeColorOverride("font_color", new Color(0f, 0.831f, 1f));
        _promptLabel.AddThemeFontSizeOverride("font_size", FontSize);
        _inputContainer.AddChild(_promptLabel);

        // 输入框
        _inputBuffer = new LineEdit
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = ModLocalization.Get("chat.placeholder", "Type a message..."),
            MouseFilter = MouseFilterEnum.Stop,
            CaretBlink = true
        };
        _inputBuffer.AddThemeColorOverride("font_color", Colors.White);
        _inputBuffer.AddThemeColorOverride("font_placeholder_color", new Color(0.5f, 0.5f, 0.5f));
        _inputBuffer.AddThemeColorOverride("caret_color", new Color(0f, 0.831f, 1f));
        _inputBuffer.AddThemeFontSizeOverride("font_size", FontSize);
        // 移除输入框的白边
#pragma warning disable CA2000 // StyleBoxFlat 所有权转移给主题系统
        var inputStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f)
        };
#pragma warning restore CA2000
        _inputBuffer.AddThemeStyleboxOverride("normal", inputStyle);
        _inputBuffer.AddThemeStyleboxOverride("focus", inputStyle);
        _inputBuffer.AddThemeStyleboxOverride("read_only", inputStyle);

        _inputBuffer.Connect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(OnTextSubmitted));
        _inputBuffer.Connect(LineEdit.SignalName.FocusEntered, Callable.From(() => _isInputFocused = true));
        _inputBuffer.Connect(LineEdit.SignalName.FocusExited, Callable.From(() => _isInputFocused = false));
        _inputBuffer.ApplyLocaleFontSubstitution(FontType.Regular, "font");
        _inputContainer.AddChild(_inputBuffer);
    }

    private void UpdateLayout()
    {
        var viewportSize = GetViewportRect().Size;
        var halfWidth = viewportSize.X / 2f;

        if (_isExpanded)
        {
            // 展开状态：显示完整历史，占半屏高度
            var expandedHeight = viewportSize.Y * 0.5f;
            OffsetLeft = 0;
            OffsetTop = -expandedHeight;
            OffsetRight = halfWidth;
            OffsetBottom = 0;

            _outputBuffer.Visible = true;
            _outputBuffer.Size = new Vector2(halfWidth, expandedHeight - InputHeight);
            _outputBuffer.Position = new Vector2(0, 0);

            _inputContainer.Visible = true;
            _inputContainer.Size = new Vector2(halfWidth, InputHeight);
            _inputContainer.Position = new Vector2(0, expandedHeight - InputHeight);
        }
        else
        {
            // 折叠状态：只显示最近1-2行
            var collapsedHeight = InputHeight * 2.5f;
            OffsetLeft = 0;
            OffsetTop = -collapsedHeight;
            OffsetRight = halfWidth;
            OffsetBottom = 0;

            _outputBuffer.Visible = true;
            _outputBuffer.Size = new Vector2(halfWidth, collapsedHeight - InputHeight);
            _outputBuffer.Position = new Vector2(0, 0);

            _inputContainer.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        // 只在折叠状态下处理淡出逻辑
        if (_isExpanded || _isFadedOut)
        {
            return;
        }

        var timeSinceLastMessage = Time.GetTicksMsec() / 1000.0 - _lastMessageTime;
        if (!(timeSinceLastMessage >= FadeOutDelaySeconds))
        {
            return;
        }

        // 计算淡出进度
        var fadeProgress = (timeSinceLastMessage - FadeOutDelaySeconds) / FadeOutDurationSeconds;
        var alpha = Mathf.Clamp(1f - (float)fadeProgress, 0f, 1f);

        _panelStyle.BgColor = new Color(0f, 0f, 0f, 0.85f * alpha);
        Modulate = new Color(1f, 1f, 1f, alpha);

        if (alpha <= 0f)
        {
            _isFadedOut = true;
        }
    }

    private void ResetFadeOut()
    {
        _lastMessageTime = Time.GetTicksMsec() / 1000.0;
        _isFadedOut = false;
        _panelStyle.BgColor = new Color(0f, 0f, 0f, 0.85f);
        Modulate = Colors.White;
    }

    public void AddMessage(ulong senderId, string senderName, string content, long timestamp)
    {
        var entry = new ChatMessageEntry(senderId, senderName, content, timestamp);
        _messages.Add(entry);

        // 限制消息数量
        while (_messages.Count > 100)
        {
            _messages.RemoveAt(0);
        }

        // 重置淡出计时器
        ResetFadeOut();

        // 渲染消息
        RenderMessages();

        // 滚动到底部
        CallDeferred(nameof(ScrollToBottom));
    }

    private void RenderMessages()
    {
        var netService = RunManager.Instance?.NetService;
        var myNetId = netService?.NetId ?? 0;

        var lines = new List<string>();

        var startIndex = _isExpanded ? 0 : Math.Max(0, _messages.Count - CollapsedVisibleLines);
        for (var i = startIndex; i < _messages.Count; i++)
        {
            var entry = _messages[i];
            var color = entry.SenderId == myNetId ? "#7fff7f" : "#ccccff";
            lines.Add($"[color={color}][{entry.SenderName}]:[/color] {EscapeBbcode(entry.Content)}");
        }

        _outputBuffer.Text = string.Join("\n", lines);
    }

    private static string EscapeBbcode(string text) =>
        text.Replace("[", "[lb]", StringComparison.Ordinal)
            .Replace("]", "[rb]", StringComparison.Ordinal);

    private void ScrollToBottom() => _outputBuffer.ScrollToLine(_outputBuffer.GetLineCount() - 1);

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true } keyEvent)
        {
            return;
        }

        // 检查是否有其他 LineEdit 持有焦点
        var focusedOwner = GetViewport()?.GuiGetFocusOwner();
        if (focusedOwner is LineEdit && focusedOwner != _inputBuffer)
        {
            // 其他输入框有焦点，忽略 Tab 键
            return;
        }

        // Tab 键切换展开/折叠
        if (keyEvent.Keycode == ToggleKey)
        {
            ToggleExpanded();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        // 展开状态下的快捷键
        if (_isExpanded)
        {
            switch (keyEvent.Keycode)
            {
                // Enter 发送消息
                case SendKey when !string.IsNullOrWhiteSpace(_inputBuffer.Text):
                    SendMessage(_inputBuffer.Text);
                    GetViewport()?.SetInputAsHandled();
                    return;
                // Escape 折叠
                case Key.Escape:
                    SetExpanded(false);
                    GetViewport()?.SetInputAsHandled();
                    return;
            }
        }
    }

    private void ToggleExpanded() => SetExpanded(!_isExpanded);

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;
        // ResetFadeOut(); // 发消息再保持吧。
        UpdateLayout();

        if (expanded)
        {
            _inputBuffer.CallDeferred(Control.MethodName.GrabFocus);
            RenderMessages(); // 重新渲染所有消息
        }
        else
        {
            _inputBuffer.ReleaseFocus();
            GetViewport()?.GuiReleaseFocus();
            RenderMessages(); // 只渲染最近几条
        }
    }

    private void OnTextSubmitted(string text) => SendMessage(text);

    private void SendMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        OnMessageSent?.Invoke(text.Trim());
        _inputBuffer.Text = "";
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            UpdateLayout();
        }
    }

    private bool _hasShownWelcome;

    public void SetVisibleForMultiplayer(bool isMultiplayer)
    {
        Visible = isMultiplayer;
        if (!isMultiplayer)
        {
            return;
        }

        ResetFadeOut();
        UpdateLayout();

        // 首次显示时添加欢迎提示
        if (!_hasShownWelcome)
        {
            _hasShownWelcome = true;
            var hint = ModLocalization.Get("chat.welcome", "Press [Tab] to open chat");
            _outputBuffer.Text = $"[color=#888888]{hint}[/color]";
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _outputBuffer?.Dispose();
            _inputBuffer?.Dispose();
            _inputContainer?.Dispose();
            _promptLabel?.Dispose();
            _panelStyle?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
///     聊天消息条目
/// </summary>
public record ChatMessageEntry(ulong SenderId, string SenderName, string Content, long Timestamp);