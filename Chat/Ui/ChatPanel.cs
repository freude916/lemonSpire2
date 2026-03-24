using System.Globalization;
using Godot;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using lemonSpire2.ColorEx;
using lemonSpire2.SendGameItem;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Localization;
using DraggableTitleBar = lemonSpire2.util.Ui.DraggableTitleBar;
using ViewportResizeNotifier = lemonSpire2.util.Ui.ViewportResizeNotifier;

namespace lemonSpire2.Chat.Ui;

/// <summary>
///     Chat panel with message display and input area.
///     Supports expand/collapse via Tab key and dragging.
/// </summary>
public sealed class ChatPanel : IDisposable
{
    private readonly Action<IIntent> _dispatch;
    private readonly List<string> _inputHistory = [];
    private readonly AudioStream? _messageSound;
    private readonly ChatModel _model;
    private readonly TooltipManager _tooltipManager = new();
    private readonly Control _tooltipParent;

    private ChatPanelContainer _container = null!;
    private double _fadeBeginTime; // 开始fade的时间点
    private bool _hasWelcome = true;
    private int _historyIndex;
    private HBoxContainer _inputContainer = null!;
    private LineEdit _inputField = null!;
    private StyleBoxFlat _inputStyle = null!;

    private bool _isExpanded;
    private bool _isUpdatingLayout; // 防止 OnResized 递归调用 UpdateLayout
    private RichTextLabel _messageBuffer = null!;
    private StyleBoxFlat _panelStyle = null!;
    private DraggableTitleBar _titleBar = null!;
    private VBoxContainer _vboxLayout = null!;

    public ChatPanel(ChatModel model, Action<IIntent> dispatch, IntentHandlerRegistry intentRegistry,
        Control tooltipParent)
    {
        _model = model;
        _dispatch = dispatch;
        _tooltipParent = tooltipParent;
        _messageSound = GD.Load<AudioStream>(ChatConfig.MessageSoundPath);
        _model.OnMessageAppended += OnMessageAppended;
        _tooltipManager.RegisterHandlers(intentRegistry);
        CreateUi();
    }


    public void Dispose()
    {
        // 取消窗口大小变化事件订阅
        ViewportResizeNotifier.Instance.OnViewportResized -= OnViewportResized;

        _model.OnMessageAppended -= OnMessageAppended;
        _panelStyle.Dispose();
        _inputStyle.Dispose();
        _container.QueueFree();
    }

    #region Model Events: Message Update Source

    private void OnMessageAppended(ChatMessage message)
    {
        DisplayMessage(message);
    }

    #endregion

    // ========== Input Handling ==========

    internal bool HandleInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true } keyEvent)
            return false;

        var focusedOwner = _container.GetViewport()?.GuiGetFocusOwner();
        if (focusedOwner is LineEdit && focusedOwner != _inputField)
            return false;

        if (keyEvent.Keycode == ChatConfig.ToggleKey)
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
                var olderText = _inputHistory[^_historyIndex];
                _inputField.Text = olderText;
                _inputField.CaretColumn = olderText.Length;
                return true;

            case Key.Down when _historyIndex > 0:
                _historyIndex--;
                var newerText = _historyIndex > 0
                    ? _inputHistory[^_historyIndex]
                    : "";
                _inputField.Text = newerText;
                _inputField.CaretColumn = newerText.Length;
                return true;
            default:
                return false;
        }
    }

    // ========== Layout & State ==========

    internal void ProcessFrame(double delta)
    {
        // Update tooltip position to follow mouse (only when preview is visible)
        if (_tooltipManager.HasPreview)
            _tooltipManager.UpdatePreviewPosition(_container.GetGlobalMousePosition());

        // 展开时不fade
        if (_isExpanded) return;

        var now = Time.GetTicksMsec() / 1000.0;

        if (now < _fadeBeginTime)
        {
            // 常亮期
            RestoreVisibility();
        }
        else
        {
            // fade期
            var fadeProgress = (now - _fadeBeginTime) / ChatConfig.FadeOutDurationSeconds;
            var alpha = Mathf.Clamp(1f - (float)fadeProgress, 0f, 1f);

            _panelStyle.BgColor = ChatConfig.GetFadedPanelBg(alpha);
            _container.Modulate = ChatConfig.GetFadedModulate(alpha);

            if (alpha <= 0.05f)
            {
                // fade完成，透传鼠标事件
                _container.Modulate = ChatConfig.GetFadedModulate(ChatConfig.FadeMinAlpha);
                _container.MouseFilter = Control.MouseFilterEnum.Ignore;
                _vboxLayout.MouseFilter = Control.MouseFilterEnum.Ignore;
                _messageBuffer.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
        }
    }

    internal void OnResized()
    {
        if (_isUpdatingLayout) return;
        UpdateLayout();
    }

    /// <summary>
    ///     推迟 Fade 计时器，在接收到新消息或折叠时调用，淡出效果
    /// </summary>
    private void DelayFadeOut(double delaySeconds)
    {
        var now = Time.GetTicksMsec() / 1000.0;
        _fadeBeginTime = Math.Max(_fadeBeginTime, now + delaySeconds);
        RestoreVisibility();
    }

    private void RestoreVisibility()
    {
        _panelStyle.BgColor = ChatConfig.PanelBgColor;
        _container.Modulate = Colors.White;
        _messageBuffer.MouseFilter = Control.MouseFilterEnum.Stop;
    }

    private void ToggleExpanded()
    {
        SetExpanded(!_isExpanded);
    }

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;

        if (expanded)
            // 展开时恢复全亮展示
            RestoreVisibility();
        else
            DelayFadeOut(0);

        UpdateLayout();

        if (expanded)
        {
            if (_hasWelcome)
            {
                _messageBuffer.Clear();
                _hasWelcome = false;
            }

            _inputField.MouseFilter = Control.MouseFilterEnum.Stop;
            _inputField.CallDeferred(Control.MethodName.GrabFocus);
        }
        else
        {
            _inputField.MouseFilter = Control.MouseFilterEnum.Ignore;
            _inputField.ReleaseFocus();
            _container.GetViewport()?.GuiReleaseFocus();
            _historyIndex = 0;
        }
    }

    internal void UpdateLayout()
    {
        if (!_container.IsInsideTree() || _isUpdatingLayout)
            return;

        _isUpdatingLayout = true;

        var viewportSize = _container.GetViewportRect().Size;
        var chatWidth = viewportSize.X * ChatConfig.PanelWidthRatio;
        float panelHeight;

        var bottomY = _container.Position.Y + _container.Size.Y;

        if (_isExpanded)
        {
            // 展开状态
            panelHeight = viewportSize.Y * ChatConfig.PanelHeightRatio;

            _titleBar.Visible = true;

            _inputContainer.Visible = true;
            _inputContainer.MouseFilter = Control.MouseFilterEnum.Stop;

            _messageBuffer.MouseFilter = Control.MouseFilterEnum.Stop;
            _inputField.MouseFilter = Control.MouseFilterEnum.Stop;

            _inputContainer.CallDeferred(Control.MethodName.GrabFocus); // TODO: Grab Focus Test
        }
        else
        {
            panelHeight = ChatConfig.FontSize * ChatConfig.CollapsedVisibleLines;
            _inputContainer.Visible = false;
            _titleBar.Visible = false;
        }

        _container.MouseFilter = Control.MouseFilterEnum.Ignore;
        _container.Size = new Vector2(chatWidth, panelHeight);

        _container.Position = new Vector2(_container.Position.X, bottomY - panelHeight); // 保持底部位置不变
        _isUpdatingLayout = false;
    }

    // ========== Message Display ==========

    private void DisplayMessage(ChatMessage message)
    {
        ChatUiPatch.Log.Debug($"DisplayMessage: segments={message.Segments.Count}, sender={message.SenderName}");

        if (_hasWelcome)
        {
            _messageBuffer.Clear();
            _hasWelcome = false;
        }

        PlayMessageSound();
        DelayFadeOut(ChatConfig.FadeOutDelaySeconds);

        var senderName = message.SenderName ?? $"Player {message.SenderId}";
        var time = message.Timestamp.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);

        // [time]
        _messageBuffer.PushColor(ChatConfig.TimeColor);
        _messageBuffer.AppendText($"[{time}] ");
        _messageBuffer.Pop();

        // sender name - 使用 ColorManager 获取玩家颜色
        var senderColor = ColorManager.Instance.GetCustomColor(message.SenderId) ?? ChatConfig.SenderColor;
        _messageBuffer.PushColor(senderColor);
        _messageBuffer.AppendText($"{senderName}: ");
        _messageBuffer.Pop();

        // message segments
        foreach (var segment in message.Segments) RenderSegment(segment);

        _messageBuffer.AppendText("\n");
        _messageBuffer.ScrollToLine(_messageBuffer.GetLineCount() - 1);
    }

    private void RenderSegment(IMsgSegment segment)
    {
        segment.RenderTo(_messageBuffer);
    }

    private void PlayMessageSound()
    {
        if (_messageSound == null || !_container.IsInsideTree())
            return;

#pragma warning disable CA2000 // AudioStreamPlayer ownership transferred to scene tree and auto-freed after playback
        var player = new AudioStreamPlayer
        {
            Stream = _messageSound,
            VolumeDb = ChatConfig.MessageSoundVolumeDb
        };
#pragma warning restore CA2000
        _container.AddChild(player);
        player.Play();
        player.Finished += player.QueueFree;
    }

    #region Ui Base

    private void CreateUi()
    {
        _container = new ChatPanelContainer(this)
        {
            Name = "ChatPanel",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            GrowVertical = Control.GrowDirection.End
        };

        _panelStyle = new StyleBoxFlat
        {
            BgColor = ChatConfig.PanelBgColor,
            BorderColor = ChatConfig.PanelBorderColor,
            BorderWidthTop = ChatConfig.BorderWidth,
            BorderWidthRight = ChatConfig.BorderWidth
        };
        _container.AddThemeStyleboxOverride("panel", _panelStyle);

        // layout
        _vboxLayout = new VBoxContainer
        {
            Name = "VBoxLayout",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _container.AddChild(_vboxLayout);

        // 标题栏（拖拽条）- 展开时显示，折叠时隐藏
        _titleBar = new DraggableTitleBar
        {
            Name = "DragBar",
            CustomMinimumSize = new Vector2(100, 24)
        };
        _titleBar.SetTitle(new LocString("gameplay_ui", "LEMONSPIRE.chat.title").GetFormattedText());
        _titleBar.SetDragTarget(_container);
        _titleBar.SetDragCallbacks(onDragEnd: () => PanelPositionHelper.ClampToViewport(_container));

        // 样式：背景色 + padding
        var titleStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.5f, 0.3f, 0.5f), // 半透明绿色便于调试
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        _titleBar.AddThemeStyleboxOverride("panel", titleStyle);

        _vboxLayout.AddChild(_titleBar);

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
        _messageBuffer.AddThemeFontSizeOverride("normal_font_size", ChatConfig.FontSize);
        _vboxLayout.AddChild(_messageBuffer);

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
        _vboxLayout.AddChild(_inputContainer);

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
        _inputField.AddThemeColorOverride("font_placeholder_color", ChatConfig.PlaceholderColor);
        _inputField.AddThemeColorOverride("caret_color", ChatConfig.CaretColor);
        _inputField.AddThemeFontSizeOverride("font_size", ChatConfig.FontSize);

        _inputStyle = new StyleBoxFlat { BgColor = ChatConfig.InputBgColor };
        _inputField.AddThemeStyleboxOverride("normal", _inputStyle);
        _inputField.AddThemeStyleboxOverride("focus", _inputStyle);
        _inputField.AddThemeStyleboxOverride("read_only", _inputStyle);

        _inputField.TextSubmitted += OnTextSubmitted;
        _inputContainer.AddChild(_inputField);

        // 初始化时隐藏拖拽条（默认折叠状态）
        _titleBar.Visible = false;
        _titleBar.CustomMinimumSize = new Vector2(0, 0);
    }

    public Control GetControl()
    {
        return _container;
    }

    public void ResetPosition()
    {
        _container.SetAnchorsPreset(Control.LayoutPreset.BottomLeft, true);

        _container.OffsetLeft = ChatConfig.PositionOffsetX;
        _container.OffsetBottom = -ChatConfig.PositionOffsetY;

        _titleBar.CustomMinimumSize = new Vector2(_vboxLayout.Size.X, _titleBar.CustomMinimumSize.Y);

        // _container.GrowVertical = Control.GrowDirection.End;
        // _container.GrowHorizontal = Control.GrowDirection.Begin;

        // This is of no use at all! GrowDirection only affects when size **increases**, but decreasing size won't move it at all
    }

    internal void Initialize()
    {
        _tooltipManager.Initialize(_tooltipParent);
        DelayFadeOut(ChatConfig.FadeOutDelaySeconds);
        UpdateLayout();
        ShowWelcome();

        // 订阅窗口大小变化事件
        ViewportResizeNotifier.Instance.OnViewportResized += OnViewportResized;

        // 注册到 InputCapture，让其放过 ChatPanel 内部的 Alt+Click
        ItemInputCapture.RegisterBlockingControl(_container);
    }

    private void OnViewportResized(Vector2 _)
    {
        PanelPositionHelper.ClampToViewport(_container);
    }

    private void ShowWelcome()
    {
        var welcomeText = new LocString("gameplay_ui", "LEMONSPIRE.chat.welcome").GetFormattedText();
        _messageBuffer.Text = $"[color=#{ChatConfig.TimeColor.ToHtml()}]{welcomeText}[/color]";
    }

    #endregion

    #region Ui Intents

    private void OnTextSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ChatUiPatch.Log.VeryDebug("should not send blank message");
            return;
        }

        text = text.Trim();

        _inputHistory.Add(text);
        if (_inputHistory.Count > ChatConfig.MaxHistoryCount)
            _inputHistory.RemoveAt(0);

        _historyIndex = 0;
        _inputField.Text = "";

        _dispatch(new IntentTextSubmit { Text = text });
        _inputField.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void OnMetaClicked(Variant meta)
    {
        _dispatch(new IntentMetaClick { Meta = meta.AsString() });
    }

    private void OnMetaHoverStarted(Variant meta)
    {
        _dispatch(new IntentMetaHoverStart
        {
            Meta = meta.AsString(),
            GlobalPosition = _container.GetGlobalMousePosition()
        });
    }

    private void OnMetaHoverEnded(Variant meta)
    {
        _dispatch(new IntentMetaHoverEnd { Meta = meta.AsString() });
    }

    #endregion
}
