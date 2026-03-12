using System.Globalization;
using Godot;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using MegaCrit.Sts2.Core.Localization;

namespace lemonSpire2.Chat.Ui;

/// <summary>
///     Chat panel with message display and input area.
///     Supports expand/collapse via Tab key.
/// </summary>
public sealed class ChatPanel : IDisposable
{

    #region ChatConfig

    private const Key ToggleKey = Key.Tab;
    private const int CollapsedVisibleLines = 4;
    private const int FontSize = 18;
    private const float InputHeight = 40f;
    private const float FadeOutDelaySeconds = 5f;
    private const float FadeOutDurationSeconds = 5f;
    private readonly Action<IIntent> _dispatch;
    private readonly List<string> _inputHistory = new();

    #endregion

    private readonly ChatModel _model;
    private readonly TooltipManager _tooltipManager = new();

    private ChatPanelContainer? _container;
    private bool _hasWelcome = true;
    private int _historyIndex;
    private HBoxContainer? _inputContainer;
    private LineEdit? _inputField;
    private bool _isExpanded;
    private double _fadeBeginTime;      // 开始fade的时间点
    private RichTextLabel? _messageBuffer;
    private StyleBoxFlat? _panelStyle;
    private StyleBoxFlat? _inputStyle;
    private readonly Control _tooltipParent;

    public ChatPanel(ChatModel model, Action<IIntent> dispatch, IntentHandlerRegistry intentRegistry, Control tooltipParent)
    {
        _model = model;
        _dispatch = dispatch;
        _tooltipParent = tooltipParent;
        _model.OnMessageAppended += OnMessageAppended;
        _tooltipManager.RegisterHandlers(intentRegistry);
        CreateUI();
    }

    public void Dispose()
    {
        _model.OnMessageAppended -= OnMessageAppended;
        _tooltipManager.Dispose();
        
        // _container?.Dispose();
        _panelStyle?.Dispose();
        _inputStyle?.Dispose();
        _inputField?.Dispose();
        _inputContainer?.Dispose();
        _messageBuffer?.Dispose();
        _container?.QueueFree();
    }

    private void CreateUI()
    {
        _container = new ChatPanelContainer(this)
        {
            Name = "ChatPanel",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        _container.AnchorLeft = 0;
        _container.AnchorTop = 1;
        _container.AnchorRight = 0;
        _container.AnchorBottom = 1;

        _panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.80f),
            BorderColor = new Color(0.2f, 0.2f, 0.2f),
            BorderWidthTop = 1,
            BorderWidthRight = 1
        };
        _container.AddThemeStyleboxOverride("panel", _panelStyle);

        // Message buffer
        _messageBuffer = new RichTextLabel
        {
            Name = "MessageBuffer",
            BbcodeEnabled = true,
            ScrollActive = true,
            ScrollFollowing = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _messageBuffer.AddThemeColorOverride("default_color", Colors.White);
        _messageBuffer.AddThemeFontSizeOverride("normal_font_size", FontSize);
        _container.AddChild(_messageBuffer);

        _messageBuffer.MetaClicked += OnMetaClicked;
        _messageBuffer.MetaHoverStarted += OnMetaHoverStarted;
        _messageBuffer.MetaHoverEnded += OnMetaHoverEnded;

        // Input container
        _inputContainer = new HBoxContainer
        {
            Name = "InputContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _container.AddChild(_inputContainer);

        // Input field (no prompt label)
        _inputField = new LineEdit
        {
            Name = "InputField",
            PlaceholderText = new LocString("gameplay_ui", "LEMONSPIRE.chat.placeholder").GetFormattedText(),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CaretBlink = true
        };
        _inputField.AddThemeColorOverride("font_color", Colors.White);
        _inputField.AddThemeColorOverride("font_placeholder_color", new Color(0.5f, 0.5f, 0.5f));
        _inputField.AddThemeColorOverride("caret_color", new Color(0f, 0.831f, 1f));
        _inputField.AddThemeFontSizeOverride("font_size", FontSize);

        _inputStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
        _inputField.AddThemeStyleboxOverride("normal", _inputStyle);
        _inputField.AddThemeStyleboxOverride("focus", _inputStyle);
        _inputField.AddThemeStyleboxOverride("read_only", _inputStyle);

        _inputField.TextSubmitted += OnTextSubmitted;
        _inputContainer.AddChild(_inputField);
    }

    public Control? GetControl() => _container;

    internal void Initialize()
    {
        _tooltipManager.Initialize(_tooltipParent);
        DelayFadeOut(FadeOutDelaySeconds);
        UpdateLayout();
        ShowWelcome();
    }

    private void ShowWelcome()
    {
        if (_messageBuffer == null) return;
        var welcomeText = new LocString("gameplay_ui", "LEMONSPIRE.chat.welcome").GetFormattedText();
        _messageBuffer.Text = $"[color=#888888]{welcomeText}[/color]";
    }

    // ========== Model Events ==========

    private void OnMessageAppended(object? sender, ChatMessage message)
    {
        DisplayMessage(message);
    }

    // ========== UI Events ==========

    private void OnTextSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        text = text.Trim();

        _inputHistory.Add(text);
        if (_inputHistory.Count > 40)
            _inputHistory.RemoveAt(0);

        _historyIndex = 0;
        _inputField!.Text = "";

        // Create message from text
        var msg = new ChatMessage
        {
            SenderId = 0, // Will be filled by ChatStore
            Timestamp = DateTime.Now,
            Segments = new List<IMsgSegment> { new RichTextSegment { Text = text } }
        };

        _dispatch(new IntentSubmit { Message = msg });
        _inputField.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void OnMetaClicked(Variant meta)
    {
        _dispatch(new IntentMetaClick { Meta = meta.AsString() });
    }

    private void OnMetaHoverStarted(Variant meta)
    {
        MainFile.Logger.Debug($"ChatPanel.OnMetaHoverStarted: meta={meta.AsString()}");
        _dispatch(new IntentMetaHoverStart
        {
            Meta = meta.AsString(),
            GlobalPosition = _container!.GetGlobalMousePosition()
        });
    }

    private void OnMetaHoverEnded(Variant meta)
    {
        _dispatch(new IntentMetaHoverEnd { Meta = meta.AsString() });
    }

    // ========== Input Handling ==========

    internal bool HandleInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true } keyEvent)
            return false;

        var focusedOwner = _container?.GetViewport()?.GuiGetFocusOwner();
        if (focusedOwner is LineEdit && focusedOwner != _inputField)
            return false;

        if (keyEvent.Keycode == ToggleKey)
        {
            ToggleExpanded();
            return true;
        }

        if (!_isExpanded) return false;
        
        switch (keyEvent.Keycode)
        {
            case Key.Escape:
                SetExpanded(false);
                return true;

            case Key.Up when _historyIndex < _inputHistory.Count:
                _historyIndex++;
                var olderText = _inputHistory[_inputHistory.Count - _historyIndex];
                _inputField!.Text = olderText;
                _inputField.CaretColumn = olderText.Length;
                return true;

            case Key.Down when _historyIndex > 0:
                _historyIndex--;
                var newerText = _historyIndex > 0
                    ? _inputHistory[_inputHistory.Count - _historyIndex]
                    : "";
                _inputField!.Text = newerText;
                _inputField.CaretColumn = newerText.Length;
                return true;
        }

        return false;
    }

    // ========== Layout & State ==========

    internal void ProcessFrame(double delta)
    {
        // Update tooltip position to follow mouse (using global coordinates)
        if (_container is not null)
        {
            _tooltipManager.UpdatePreviewPosition(_container.GetGlobalMousePosition());
        }

        // 展开时不fade
        if (_isExpanded || _panelStyle == null || _container == null) return;

        var now = Time.GetTicksMsec() / 1000.0;

        if (now < _fadeBeginTime)
        {
            // 常亮期
            RestoreVisibility();
        }
        else
        {
            // fade期
            var fadeProgress = (now - _fadeBeginTime) / FadeOutDurationSeconds;
            var alpha = Mathf.Clamp(1f - (float)fadeProgress, 0f, 1f);

            _panelStyle.BgColor = new Color(0f, 0f, 0f, 0.80f * alpha);
            _container.Modulate = new Color(1f, 1f, 1f, alpha);

            if (alpha <= 0f)
            {
                // fade完成，透传鼠标事件
                _container.Modulate = new Color(1f, 1f, 1f, 0.01f);
                _container.MouseFilter = Control.MouseFilterEnum.Ignore;
                _messageBuffer!.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
        }
    }

    internal void OnResized()
    {
        UpdateLayout();
    }

    /// <summary>
    /// 重置计时器，收到消息时调用
    /// </summary>
    private void DelayFadeOut(double delaySeconds)
    {
        var now = Time.GetTicksMsec() / 1000.0;
        _fadeBeginTime = Math.Max(_fadeBeginTime, now + delaySeconds);
        RestoreVisibility();
    }

    /// <summary>
    /// 恢复显示状态，准备 fade
    /// </summary>
    private void RestoreVisibility()
    {
        _panelStyle!.BgColor = new Color(0f, 0f, 0f, 0.80f);
        _container!.Modulate = Colors.White;
        _messageBuffer!.MouseFilter = Control.MouseFilterEnum.Stop;
    }

    private void ToggleExpanded()
    {
        SetExpanded(!_isExpanded);
    }

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;

        if (expanded)
        {
            // 展开时恢复全亮展示
            RestoreVisibility();
        }
        else
        {
            DelayFadeOut(0);
        }

        UpdateLayout();

        if (expanded)
        {
            if (_hasWelcome && _messageBuffer != null)
            {
                _messageBuffer.Clear();
                _hasWelcome = false;
            }

            _inputField!.MouseFilter = Control.MouseFilterEnum.Stop;
            _inputField.CallDeferred(Control.MethodName.GrabFocus);
        }
        else
        {
            _inputField!.MouseFilter = Control.MouseFilterEnum.Ignore;
            _inputField.ReleaseFocus();
            _container?.GetViewport()?.GuiReleaseFocus();
            _historyIndex = 0;
        }
    }

    internal void UpdateLayout()
    {
        if (_container == null || _messageBuffer == null || !_container.IsInsideTree())
            return;

        const float Margin = 5f;
        var viewportSize = _container.GetViewportRect().Size;
        var chatWidth = viewportSize.X * 0.3f;

        if (_isExpanded)
        {
            var expandedHeight = viewportSize.Y * 0.5f;
            _container.OffsetLeft = Margin;
            _container.OffsetTop = -expandedHeight - Margin;
            _container.OffsetRight = chatWidth;
            _container.OffsetBottom = -Margin;

            var msgHeight = expandedHeight - InputHeight;
            _messageBuffer.Size = new Vector2(chatWidth, msgHeight);
            _messageBuffer.Position = new Vector2(0, 0);
            _messageBuffer.MouseFilter = Control.MouseFilterEnum.Stop;

            _inputContainer!.Visible = true;
            _inputContainer.Size = new Vector2(chatWidth, InputHeight);
            _inputContainer.Position = new Vector2(0, msgHeight);
            _inputContainer.MouseFilter = Control.MouseFilterEnum.Stop;

            _inputField!.MouseFilter = Control.MouseFilterEnum.Stop;
            _container.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        else
        {
            var collapsedHeight = FontSize * CollapsedVisibleLines;
            _container.OffsetLeft = Margin;
            _container.OffsetTop = -collapsedHeight - Margin;
            _container.OffsetRight = chatWidth;
            _container.OffsetBottom = -Margin;

            _messageBuffer.Size = new Vector2(chatWidth, collapsedHeight);
            _messageBuffer.Position = new Vector2(0, 0);
            _messageBuffer.MouseFilter = Control.MouseFilterEnum.Stop;

            _inputContainer!.Visible = false;
            _container.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    // ========== Message Display ==========

    private void DisplayMessage(ChatMessage message)
    {
        MainFile.Logger.Debug($"DisplayMessage: segments={message.Segments.Count}, sender={message.SenderName}");
        
        if (_messageBuffer == null) return;

        if (_hasWelcome)
        {
            _messageBuffer.Clear();
            _hasWelcome = false;
        }

        DelayFadeOut(FadeOutDelaySeconds);

        var senderName = message.SenderName ?? $"Player {message.SenderId}";
        var time = message.Timestamp.ToString("HH:mm", CultureInfo.InvariantCulture);

        var color = new Color(0.8f, 0.8f, 1f); // #ccccff

        // [time]
        _messageBuffer.PushColor(new Color(0.53f, 0.53f, 0.53f)); // #888888
        _messageBuffer.AppendText($"[{time}] ");
        _messageBuffer.Pop();

        // sender name
        _messageBuffer.PushColor(color);
        _messageBuffer.AppendText($"{senderName}: ");
        _messageBuffer.Pop();

        // message segments
        foreach (var segment in message.Segments) RenderSegment(segment);

        _messageBuffer.AppendText("\n");
        _messageBuffer.ScrollToLine(_messageBuffer.GetLineCount() - 1);
    }

    private void RenderSegment(IMsgSegment segment)
    {
        if (_messageBuffer is null) return;
        segment.RenderTo(_messageBuffer);
    }
}

/// <summary>
///     Internal container handling input events and frame updates.
/// </summary>
internal sealed partial class ChatPanelContainer(ChatPanel owner) : Panel
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        owner.Initialize();
    }

    public override void _ExitTree()
    {
        owner.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (owner.HandleInput(@event))
            GetViewport()?.SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        owner.ProcessFrame(delta);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            owner.OnResized();
    }
}