using Godot;
using lemonSpire2.SendGameItem;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using DraggableTitleBar = lemonSpire2.util.Ui.DraggableTitleBar;
using ViewportResizeNotifier = lemonSpire2.util.Ui.ViewportResizeNotifier;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using Vector2 = Godot.Vector2;

namespace lemonSpire2.PlayerStateEx.OverlayPanel;

/// <summary>
///     玩家悬浮面板
///     显示其他玩家的手牌、药水等信息
///     支持拖拽、Alt+Click 发送物品
/// </summary>
public partial class PlayerOverlayPanel : Control
{
    #region Constants & Fields

    private const float MinContentHeight = 80f;
    private const float PanelWidth = 280f;
    private const float HeightUpdateEpsilon = 0.5f;

    private static Logger Log => PlayerPanelRegistry.Log;

    private readonly HashSet<string> _pendingProviderUpdates = [];
    private readonly Dictionary<string, Control> _providerContents = [];
    private readonly List<Action> _unsubscribeContentActions = [];
    private readonly List<Action> _unsubscribeVisibilityActions = [];

    private Player _player = null!;
    private PanelContainer _panel = null!;
    private VBoxContainer _mainContainer = null!;
    private DraggableTitleBar _header = null!;
    private Label _headerTitle = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _contentContainer = null!;

    private float _lastAppliedTargetHeight = -1f;
    private bool _needsLayoutUpdate = true;
    private bool _needsRefresh;
    private Action<Vector2>? _onViewportResized;

    #endregion

    #region Godot Lifecycle

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CreateUi();

        // 注册到 InputCapture，让其放过 Panel 内部的 Alt+Click
        ItemInputCapture.RegisterBlockingControl(_panel);

        _onViewportResized = _ =>
        {
            _needsLayoutUpdate = true;
            PanelPositionHelper.ClampToViewport(_panel);
        };
        ViewportResizeNotifier.Instance.OnViewportResized += _onViewportResized;
    }

    public override void _Process(double delta)
    {
        if (_needsRefresh)
        {
            _needsRefresh = false;
            _pendingProviderUpdates.Clear();
            RefreshAllProviders();
        }

        if (!_needsRefresh && _pendingProviderUpdates.Count > 0)
            FlushPendingProviderUpdates();

        UpdatePanelSize();
    }

    public override void _ExitTree()
    {
        if (_onViewportResized != null)
            ViewportResizeNotifier.Instance.OnViewportResized -= _onViewportResized;

        CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;

        ClearProviderContents();
    }

    #endregion

    #region Initialization & Factory

    public void Initialize(Player player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _headerTitle.Text = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);

        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        // 保留战斗结束刷新，确保奖励等区块能在状态切换后重建。

        RefreshAllProviders();
    }

    public static PlayerOverlayPanel Show(Player player, Vector2? position = null)
    {
        ArgumentNullException.ThrowIfNull(player);

        var panel = new PlayerOverlayPanel
        {
            Name = $"PlayerOverlayPanel_{player.NetId}"
        };

        NRun.Instance?.GlobalUi.AddChild(panel);
        panel.Initialize(player);

        if (position.HasValue) panel.Position = position.Value;

        return panel;
    }

    #endregion

    #region Provider Management

    private void CreateProviderContents()
    {
        foreach (var provider in PlayerPanelRegistry.GetProviders())
        {
            var providerId = provider.Id;
            var unsubscribeVisibility =
                provider.SubscribeVisibilityEvents(_player, () => { QueueProviderUpdate(providerId); });
            if (unsubscribeVisibility != null) _unsubscribeVisibilityActions.Add(unsubscribeVisibility);

            if (!provider.ShouldShow(_player)) continue;

            // 创建区块容器
            var sectionContainer = new VBoxContainer
            {
                Name = $"Section_{providerId}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            sectionContainer.AddThemeConstantOverride("separation", 4);

            // 区块标题
            var sectionTitle = new Label
            {
                Text = provider.DisplayName,
                MouseFilter = MouseFilterEnum.Ignore
            };
            sectionTitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
            sectionTitle.AddThemeFontSizeOverride("font_size", 20);
            sectionContainer.AddChild(sectionTitle);

            _contentContainer.AddChild(sectionContainer);

            var content = provider.CreateContent(_player);
            _providerContents[providerId] = content;
            sectionContainer.AddChild(content);

            provider.UpdateContent(_player, content);

            var unsubscribeContent = provider.SubscribeEvents(_player, () =>
            {
                // 事件可能在数据真正落地前触发；延迟到下一帧读取可避免"一拍慢"。
                QueueProviderUpdate(providerId);
            });
            if (unsubscribeContent != null) _unsubscribeContentActions.Add(unsubscribeContent);
        }
    }

    private void ClearProviderContents()
    {
        foreach (var unsubscribe in _unsubscribeContentActions) unsubscribe();
        _unsubscribeContentActions.Clear();
        foreach (var unsubscribe in _unsubscribeVisibilityActions) unsubscribe();
        _unsubscribeVisibilityActions.Clear();
        _pendingProviderUpdates.Clear();

        foreach (var entry in _providerContents)
        {
            var provider = PlayerPanelRegistry.GetProvider(entry.Key);
            provider?.Cleanup(entry.Value);
        }

        _providerContents.Clear();

        foreach (var child in _contentContainer.GetChildren())
        {
            _contentContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    #endregion

    #region Refresh Logic

    public void Refresh()
    {
        _needsRefresh = true;
    }

    private void RefreshAllProviders()
    {
        ClearProviderContents();
        CreateProviderContents();

        _needsLayoutUpdate = true;
        _lastAppliedTargetHeight = -1f;
        PanelPositionHelper.ClampToViewport(_panel);
    }

    private void QueueProviderUpdate(string providerId)
    {
        if (string.IsNullOrEmpty(providerId)) return;
        _pendingProviderUpdates.Add(providerId);
    }

    private void FlushPendingProviderUpdates()
    {
        if (_pendingProviderUpdates.Count == 0) return;

        var pendingProviderIds = _pendingProviderUpdates.ToArray();
        _pendingProviderUpdates.Clear();

        foreach (var providerId in pendingProviderIds)
        {
            var provider = PlayerPanelRegistry.GetProvider(providerId);
            if (provider == null) continue;

            var shouldShow = provider.ShouldShow(_player);
            var hasContent = _providerContents.ContainsKey(providerId);

            if (shouldShow != hasContent)
            {
                Refresh();
                return;
            }

            if (shouldShow && _providerContents.TryGetValue(providerId, out var content))
                provider.UpdateContent(_player, content);
        }

        _needsLayoutUpdate = true;
    }

    #endregion

    #region UI & Layout

    private void CreateUi()
    {
        _panel = new PanelContainer
        {
            Name = "Panel",
            AnchorsPreset = (int)LayoutPreset.TopLeft
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f),
            BorderColor = new Color(0.4f, 0.4f, 0.5f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);

        _mainContainer = new VBoxContainer
        {
            Name = "MainContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _panel.AddChild(_mainContainer);

        _header = new DraggableTitleBar { Name = "Header" };
        _header.SetDragTarget(_panel);
        _header.SetDragCallbacks(onDragEnd: () => PanelPositionHelper.ClampToViewport(_panel));
        _headerTitle = _header.GetTitleLabel();
        _header.ShowCloseButton(OnCloseButtonPressed);
        _mainContainer.AddChild(_header);

        var separator = new HSeparator { Name = "Separator" };
        separator.AddThemeColorOverride("separator_color", new Color(0.3f, 0.3f, 0.4f));
        _mainContainer.AddChild(separator);

        _scrollContainer = new ScrollContainer
        {
            Name = "ScrollContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        _mainContainer.AddChild(_scrollContainer);

        _contentContainer = new VBoxContainer
        {
            Name = "ContentContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _contentContainer.AddThemeConstantOverride("separation", 8);
        _scrollContainer.AddChild(_contentContainer);
    }

    private void UpdatePanelSize()
    {
        var contentHeight = _contentContainer.GetCombinedMinimumSize().Y;
        var maxHeight = GetMaxContentHeight();
        var targetHeight = Mathf.Clamp(contentHeight, MinContentHeight, maxHeight);
        var shouldApplyHeight = _needsLayoutUpdate ||
                                Mathf.Abs(targetHeight - _lastAppliedTargetHeight) > HeightUpdateEpsilon;

        if (!shouldApplyHeight) return;

        _needsLayoutUpdate = false;
        _lastAppliedTargetHeight = targetHeight;

        // Godot 容器在缩小时可能保留上一次布局结果，先归零再设置 min size 能确保及时收缩。
        _panel.Size = Vector2.Zero;
        _mainContainer.Size = Vector2.Zero;
        _scrollContainer.Size = Vector2.Zero;
        _contentContainer.Size = Vector2.Zero;
        _scrollContainer.CustomMinimumSize = new Vector2(PanelWidth, targetHeight);
        PanelPositionHelper.ClampToViewport(_panel);
    }

    private float GetMaxContentHeight()
    {
        var viewportHeight = GetViewport()?.GetVisibleRect().Size.Y ?? 1080f;
        return viewportHeight * 0.5f;
    }

    #endregion

    #region Event Handlers

    private void OnCloseButtonPressed()
    {
        Hide();
        QueueFree();
    }

    private void OnCombatSetUp(CombatState _)
    {
        Log.Debug("CombatSetUp, refreshing");
        Refresh();
    }

    private void OnCombatEnded(CombatRoom _)
    {
        Log.Debug("CombatEnded, refreshing");
        Refresh();
    }

    #endregion
}
