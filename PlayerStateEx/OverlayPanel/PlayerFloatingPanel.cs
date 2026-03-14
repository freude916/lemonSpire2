using Godot;
using lemonSpire2.PlayerStateEx.ShopEx;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using DraggableTitleBar = lemonSpire2.util.Ui.DraggableTitleBar;
using ViewportResizeNotifier = lemonSpire2.util.Ui.ViewportResizeNotifier;

namespace lemonSpire2.PlayerStateEx.OverlayPanel;

/// <summary>
///     玩家悬浮面板
///     显示其他玩家的手牌、药水等信息
///     支持拖拽、Alt+Click 发送物品
/// </summary>
public partial class PlayerOverlayPanel : Control
{
    private const float MinContentHeight = 80f;
    private const float PanelWidth = 400f;
    private readonly Dictionary<string, Control> _providerContents = [];
    private readonly List<Action> _unsubscribeActions = [];
    private readonly HashSet<string> _visibleProviders = [];

    private VBoxContainer? _contentContainer;
    private DraggableTitleBar? _header;
    private Label? _headerTitle;
    private VBoxContainer? _mainContainer;
    private PanelContainer? _panel;
    private Player? _player;
    private ScrollContainer? _scrollContainer;

    private bool _needsRefresh;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CreateUi();
        ViewportResizeNotifier.Instance.OnViewportResized += OnViewportResized;
    }

    private void OnViewportResized(Vector2 _)
    {
        UpdatePanelHeight();
        PanelPositionHelper.ClampToViewport(_panel);
    }

    public override void _Process(double delta)
    {
        if (!_needsRefresh) return;
        _needsRefresh = false;

        if (_player == null) return;
        RefreshAllProviders();
    }

    private void CreateUi()
    {
        _panel = new PanelContainer
        {
            Name = "Panel",
            AnchorsPreset = (int)LayoutPreset.TopLeft,
            CustomMinimumSize = new Vector2(PanelWidth, MinContentHeight)
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

        // 始终使用 ScrollContainer，不设置 SizeFlagsVertical = ExpandFill
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

    /// <summary>
    ///     更新面板高度
    ///     动态调整 ScrollContainer 的最低高度，并强制清空 Size 让 Godot 自动重新排版
    /// </summary>
    private void UpdatePanelHeight()
    {
        if (_scrollContainer == null || _contentContainer == null || _panel == null || _mainContainer == null) return;

        // 使用 GetMinimumSize() 获取实时所需高度，而非 Size.Y（可能是旧的）
        var contentHeight = _contentContainer.GetMinimumSize().Y;
        var maxHeight = GetMaxContentHeight();

        // 钳制目标高度：[MinContentHeight, maxHeight]
        var targetHeight = Mathf.Clamp(contentHeight, MinContentHeight, maxHeight);

        // 只控制 ScrollContainer 的 CustomMinimumSize
        _scrollContainer.CustomMinimumSize = new Vector2(0, targetHeight);

        // 核心技巧：重置所有父级容器的 Size，让 Godot 布局引擎重新计算
        // 这样容器才能在内容减少时自动缩小
        _scrollContainer.Size = Vector2.Zero;
        _mainContainer.Size = Vector2.Zero;
        _panel.Size = Vector2.Zero;
    }

    private float GetMaxContentHeight()
    {
        var viewportHeight = GetViewport()?.GetVisibleRect().Size.Y ?? 1080f;
        return viewportHeight * 0.5f;
    }

    public void Initialize(Player player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _headerTitle!.Text = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);

        PlayerPanelRegistry.Initialize();

        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;

        RefreshAllProviders();
    }

    /// <summary>
    ///     刷新所有 Provider（重建整个内容）
    /// </summary>
    private void RefreshAllProviders()
    {
        if (_player == null || _contentContainer == null) return;

        ClearProviderContents();
        CreateProviderContents();
        UpdatePanelHeight();
        PanelPositionHelper.ClampToViewport(_panel);
    }

    private void CreateProviderContents()
    {
        if (_player == null || _contentContainer == null) return;

        foreach (var provider in PlayerPanelRegistry.GetProviders())
        {
            if (!provider.ShouldShow(_player)) continue;

            _visibleProviders.Add(provider.ProviderId);

            // 创建区块容器
            var sectionContainer = new VBoxContainer
            {
                Name = $"Section_{provider.ProviderId}",
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

            // 先添加到场景树，这样后续创建的内容才能正确触发 _Ready
            _contentContainer.AddChild(sectionContainer);

            // 创建内容（此时 sectionContainer 已在场景树中）
            var content = provider.CreateContent(_player);
            _providerContents[provider.ProviderId] = content;
            sectionContainer.AddChild(content);

            // content 现在在场景树中了，可以安全地更新内容
            provider.UpdateContent(_player, content);

            var unsubscribe = provider.SubscribeEvents(_player, () => OnProviderContentChanged(provider));
            if (unsubscribe != null) _unsubscribeActions.Add(unsubscribe);
        }

        // 订阅 ShopManager 事件，用于检测 Provider 可见性变化
        ShopManager.Instance.InventoryUpdated += OnShopInventoryUpdated;
    }

    private void OnProviderContentChanged(IPlayerPanelProvider provider)
    {
        if (_player == null) return;

        // 先更新内容
        if (_providerContents.TryGetValue(provider.ProviderId, out var content))
            provider.UpdateContent(_player, content);

        // 延迟更新高度（等待布局系统计算）
        CallDeferred(nameof(UpdatePanelHeight));
    }

    private void OnShopInventoryUpdated(ulong netId)
    {
        if (_player != null && netId == _player.NetId)
            _needsRefresh = true;
    }

    private void ClearProviderContents()
    {
        // 取消事件订阅
        foreach (var unsubscribe in _unsubscribeActions) unsubscribe();
        _unsubscribeActions.Clear();

        // 取消 ShopManager 事件订阅
        ShopManager.Instance.InventoryUpdated -= OnShopInventoryUpdated;

        // 清除 UI
        foreach (var content in _providerContents.Values) content.QueueFree();
        _providerContents.Clear();
        _visibleProviders.Clear();

        if (_contentContainer == null) return;

        foreach (var child in _contentContainer.GetChildren())
            child.QueueFree();
    }

    private void OnCloseButtonPressed()
    {
        Hide();
        QueueFree();
    }

    public override void _ExitTree()
    {
        ViewportResizeNotifier.Instance.OnViewportResized -= OnViewportResized;
        CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        ClearProviderContents();
    }

    private void OnCombatSetUp(CombatState _)
    {
        MainFile.Logger.Debug("[PlayerOverlayPanel] CombatSetUp, refreshing");
        _needsRefresh = true;
    }

    private void OnCombatEnded(CombatRoom _)
    {
        MainFile.Logger.Debug("[PlayerOverlayPanel] CombatEnded, refreshing");
        _needsRefresh = true;
    }

    public void Refresh()
    {
        _needsRefresh = true;
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
}
