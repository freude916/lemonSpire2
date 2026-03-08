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

    private const int CollapsedVisibleLines = 4;
    private const int FontSize = 18;
    private const float InputHeight = 40f;
    private const float FadeOutDelaySeconds = 5f;
    private const float FadeOutDurationSeconds = 5f;

    private readonly List<ChatMessageEntry> _messages = [];

    private bool _hasShownWelcome;
    private AudioStream? _messageSound;
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
        _messageSound = GD.Load<AudioStream>("res://lemonSpire2/receive-message.mp3");

        // Anchor to bottom-left corner
        AnchorLeft = 0;
        AnchorTop = 1;
        AnchorRight = 0;
        AnchorBottom = 1;

        // Collapsed by default and allow mouse events to pass through
        MouseFilter = MouseFilterEnum.Ignore;

        CreateUi();
        UpdateLayout();
    }

    private void CreateUi()
    {
        // Learned from Console
        
        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.80f),
            BorderColor = new Color(0.2f, 0.2f, 0.2f),
            BorderWidthTop = 1,
            BorderWidthRight = 1
        };
        AddThemeStyleboxOverride("panel", _panelStyle);

        // Text area
        _outputBuffer = new RichTextLabel
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            BbcodeEnabled = true,
            ScrollActive = true,
            ScrollFollowing = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _outputBuffer.AddThemeColorOverride("default_color", Colors.White);
        _outputBuffer.AddThemeFontSizeOverride("normal_font_size", FontSize);
        AddChild(_outputBuffer);

        // Input area
        _inputContainer = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_inputContainer);

        // Prompt
        _promptLabel = new Label
        {
            Text = "➜",
            CustomMinimumSize = new Vector2(30, 0)
        };
        _promptLabel.AddThemeColorOverride("font_color", new Color(0f, 0.831f, 1f));
        _promptLabel.AddThemeFontSizeOverride("font_size", FontSize);
        _inputContainer.AddChild(_promptLabel);

        // Input
        _inputBuffer = new LineEdit
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = ModLocalization.Get("chat.placeholder", "Type a message..."),
            MouseFilter = MouseFilterEnum.Ignore,
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
        var chatUiWidth = viewportSize.X * 0.3f;

        if (_isExpanded)
        {
            // 展开状态：显示完整历史，占半屏高度
            var expandedHeight = viewportSize.Y * 0.5f;
            OffsetLeft = 0;
            OffsetTop = -expandedHeight;
            OffsetRight = chatUiWidth;
            OffsetBottom = 0;

            _outputBuffer.Visible = true;
            _outputBuffer.Size = new Vector2(chatUiWidth, expandedHeight - InputHeight);
            _outputBuffer.Position = new Vector2(0, 0);

            _inputContainer.Visible = true;
            _inputContainer.Size = new Vector2(chatUiWidth, InputHeight);
            _inputContainer.Position = new Vector2(0, expandedHeight - InputHeight);
        }
        else
        {
            // 折叠状态：只显示最近1-2行
            var collapsedHeight = FontSize * CollapsedVisibleLines;
            OffsetLeft = 0;
            OffsetTop = -collapsedHeight;
            OffsetRight = chatUiWidth;
            OffsetBottom = 0;

            _outputBuffer.Visible = true;
            _outputBuffer.Size = new Vector2(chatUiWidth, collapsedHeight - InputHeight);
            _outputBuffer.Position = new Vector2(0, 0);

            _inputContainer.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        // 展开状态不处理淡出
        if (_isExpanded)
        {
            return;
        }

        // 已完全淡出，保持最小可见性以接收输入
        if (_isFadedOut)
        {
            return;
        }

        var timeSinceLastMessage = Time.GetTicksMsec() / 1000.0 - _lastMessageTime;
        if (timeSinceLastMessage < FadeOutDelaySeconds)
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
            // 保持极低可见性以确保能接收输入事件
            Modulate = new Color(1f, 1f, 1f, 0.01f);
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

        // 播放音效
        PlayMessageSound();

        // 重置淡出计时器
        ResetFadeOut();

        // 渲染消息
        RenderMessages();

        // 滚动到底部
        CallDeferred(nameof(ScrollToBottom));
    }

    private void PlayMessageSound()
    {
        if (_messageSound == null)
        {
            return;
        }

#pragma warning disable CA2000 // AudioStreamPlayer 所有权转移到场景树，播放完成后由 QueueFree 释放
        var player = new AudioStreamPlayer
        {
            Stream = _messageSound,
            VolumeDb = -6f // 稍微降低音量
        };
#pragma warning restore CA2000
        AddChild(player);
        player.Play();
        player.Finished += player.QueueFree;
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
        ResetFadeOut(); // 展开时重置淡出状态
        UpdateLayout();

        if (expanded)
        {
            // 展开时拦截鼠标事件
            MouseFilter = MouseFilterEnum.Stop;
            _outputBuffer.MouseFilter = MouseFilterEnum.Stop;
            _inputContainer.MouseFilter = MouseFilterEnum.Stop;
            _inputBuffer.MouseFilter = MouseFilterEnum.Stop;
            
            _inputBuffer.CallDeferred(Control.MethodName.GrabFocus);
            RenderMessages(); // 重新渲染所有消息
        }
        else
        {
            // 折叠时让鼠标穿透
            MouseFilter = MouseFilterEnum.Ignore;
            _outputBuffer.MouseFilter = MouseFilterEnum.Ignore;
            _inputContainer.MouseFilter = MouseFilterEnum.Ignore;
            _inputBuffer.MouseFilter = MouseFilterEnum.Ignore;
            
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