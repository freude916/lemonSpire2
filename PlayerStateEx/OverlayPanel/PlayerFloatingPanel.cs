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
    private readonly Dictionary<string, Control> _providerContents = [];
    private readonly List<Action> _unsubscribeActions = [];

    // 追踪上次 provider 可见状态
    private readonly HashSet<string> _visibleProviders = [];
    private VBoxContainer? _contentContainer;
    private DraggableTitleBar? _header;
    private Label? _headerTitle;
    private VBoxContainer? _mainContainer;

    // 脏标记：仅在事件触发时检查可见性变化
    private bool _needsVisibilityCheck;
    private PanelContainer? _panel;
    private Player? _player;
    private ScrollContainer? _scrollContainer;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;

        CreateUi();

        // 订阅窗口大小变化事件
        ViewportResizeNotifier.Instance.OnViewportResized += OnViewportResized;
    }

    private void OnViewportResized(Vector2 _)
    {
        PanelPositionHelper.ClampToViewport(_panel);
    }

    public override void _Process(double delta)
    {
        // 仅在事件触发时检查，避免每帧轮询
        if (!_needsVisibilityCheck) return;
        _needsVisibilityCheck = false;

        if (_player == null) return;

        // 检查是否有新增的 provider
        var needsRebuild = (from provider in PlayerPanelRegistry.GetProviders()
            let shouldShow = provider.ShouldShow(_player)
            let wasVisible = _visibleProviders.Contains(provider.ProviderId)
            where shouldShow != wasVisible select shouldShow).Any();

        if (!needsRebuild) return;

        MainFile.Logger.Debug("[PlayerOverlayPanel] Provider visibility changed, rebuilding contents");
        RebuildProviderContents();
    }

    private void CreateUi()
    {
        // 主面板
        _panel = new PanelContainer
        {
            Name = "Panel",
            AnchorsPreset = (int)LayoutPreset.TopLeft,
            CustomMinimumSize = new Vector2(280, 0)  // 只设置最小宽度，高度自适应
        };

        // 添加样式
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

        // 主容器
        _mainContainer = new VBoxContainer
        {
            Name = "MainContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _panel.AddChild(_mainContainer);

        // 标题栏（使用 DraggableHeader）
        _header = new DraggableTitleBar
        {
            Name = "Header"
        };
        _header.SetDragTarget(_panel);
        _header.SetDragCallbacks(onDragEnd: () => PanelPositionHelper.ClampToViewport(_panel));
        _headerTitle = _header.GetTitleLabel(); // 保存标题引用
        _header.ShowCloseButton(OnCloseButtonPressed);
        _mainContainer.AddChild(_header);

        // 分隔线
        var separator = new HSeparator
        {
            Name = "Separator"
        };
        separator.AddThemeColorOverride("separator_color", new Color(0.3f, 0.3f, 0.4f));
        _mainContainer.AddChild(separator);

        // 内容容器
        _contentContainer = new VBoxContainer
        {
            Name = "ContentContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _contentContainer.AddThemeConstantOverride("separation", 8);
        _mainContainer.AddChild(_contentContainer);
    }

    private float GetMaxContentHeight()
    {
        var viewportHeight = GetViewport()?.GetVisibleRect().Size.Y ?? 1080f;
        return viewportHeight * 0.5f;
    }

    private void ApplyHeightLimit()
    {
        if (_panel == null || _contentContainer == null) return;

        var maxHeight = GetMaxContentHeight();
        var contentHeight = _contentContainer.Size.Y;

        // 计算标题栏和分隔线的高度
        var headerHeight = _header?.Size.Y ?? 0;
        const float separatorHeight = 10f; // 分隔线大概高度
        var totalHeight = contentHeight + headerHeight + separatorHeight + 8f; // 8 是 padding

        if (totalHeight > maxHeight)
        {
            // 内容超出限制，需要包装成 ScrollContainer
            WrapInScrollContainer(maxHeight - headerHeight - separatorHeight - 8f);
        }
        else
        {
            // 内容未超出限制，确保不使用 ScrollContainer
            UnwrapFromScrollContainer();
        }
    }

    private void WrapInScrollContainer(float maxHeight)
    {
        if (_contentContainer == null || _mainContainer == null) return;

        // 检查是否已经包装
        if (_scrollContainer != null && _scrollContainer.GetParent() == _mainContainer) return;

        // 从 mainContainer 移除 contentContainer
        _mainContainer.RemoveChild(_contentContainer);

        // 创建 ScrollContainer
        _scrollContainer = new ScrollContainer
        {
            Name = "ScrollContainer",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            CustomMinimumSize = new Vector2(0, maxHeight)
        };

        _scrollContainer.AddChild(_contentContainer);
        _mainContainer.AddChild(_scrollContainer);
    }

    private void UnwrapFromScrollContainer()
    {
        if (_contentContainer == null || _mainContainer == null) return;

        // 检查是否在 ScrollContainer 中
        if (_scrollContainer == null || _scrollContainer.GetParent() != _mainContainer) return;

        // 从 ScrollContainer 移除 contentContainer
        _scrollContainer.RemoveChild(_contentContainer);

        // 移除并销毁 ScrollContainer
        _mainContainer.RemoveChild(_scrollContainer);
        _scrollContainer.QueueFree();
        _scrollContainer = null;

        // 直接添加回 mainContainer
        _mainContainer.AddChild(_contentContainer);
    }

    /// <summary>
    ///     初始化面板
    /// </summary>
    public void Initialize(Player player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _headerTitle!.Text = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);

        // 初始化 Provider 注册表
        PlayerPanelRegistry.Initialize();

        // 订阅战斗状态变化事件
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;

        // 创建内容
        CreateProviderContents();
    }

    private void CreateProviderContents()
    {
        if (_player == null || _contentContainer == null) return;

        // 清除现有内容
        ClearProviderContents();

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

            // 订阅事件
            var unsubscribe = provider.SubscribeEvents(_player, () =>
            {
                UpdateProviderContent(provider);
                _needsVisibilityCheck = true; // 事件触发时检查可见性
            });
            if (unsubscribe != null) _unsubscribeActions.Add(unsubscribe);
        }

        // 订阅 ShopManager 事件，确保即使 ShopProvider 未创建也能检测到数据变化
        ShopManager.Instance.InventoryUpdated += OnShopInventoryUpdated;
    }

    private void OnShopInventoryUpdated(ulong netId)
    {
        // 当商店数据更新时，检查是否有新 Provider 需要显示
        if (_player != null && netId == _player.NetId)
        {
            MainFile.Logger.Debug($"[PlayerOverlayPanel] OnShopInventoryUpdated for player {_player.NetId}");
            _needsVisibilityCheck = true;
        }
    }

    private void RebuildProviderContents()
    {
        // 只重置宽度，让高度自适应内容
        _panel!.Size = new Vector2(400, 40);
        _panel!.CustomMinimumSize = new Vector2(280, 40);

        // 重新构建所有内容
        CreateProviderContents();

        // 延迟应用高度限制，让布局系统先计算
        CallDeferred(nameof(ApplyHeightLimit));

        // 重新 clamp 到视口
        PanelPositionHelper.ClampToViewport(_panel);
    }

    /// <summary>
    ///     刷新面板内容（用于手动刷新）
    /// </summary>
    public void Refresh()
    {
        RebuildProviderContents();
    }

    private void UpdateProviderContent(IPlayerPanelProvider provider)
    {
        if (_player == null) return;

        if (_providerContents.TryGetValue(provider.ProviderId, out var content))
            provider.UpdateContent(_player, content);

        // 延迟重置面板大小，让布局系统先更新
        CallDeferred(nameof(ResetPanelSize));
    }

    private void ResetPanelSize()
    {
        // 重置整个容器链的大小，让布局系统重新计算
        if (_contentContainer != null)
        {
            _contentContainer.Size = Vector2.Zero;
            _contentContainer.CustomMinimumSize = Vector2.Zero;
        }

        if (_mainContainer != null)
        {
            _mainContainer.Size = Vector2.Zero;
            _mainContainer.CustomMinimumSize = Vector2.Zero;
        }

        if (_panel != null)
        {
            _panel.Size = Vector2.Zero;
            _panel.CustomMinimumSize = Vector2.Zero;
        }

        // 延迟应用高度限制
        CallDeferred(nameof(ApplyHeightLimit));
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
        _visibleProviders.Clear();  // 清空可见 provider 状态

        if (_contentContainer != null)
        {
            foreach (var child in _contentContainer.GetChildren())
                child.QueueFree();
            // 重置容器大小
            _contentContainer.Size = Vector2.Zero;
            _contentContainer.CustomMinimumSize = Vector2.Zero;
        }
    }

    private void OnCloseButtonPressed()
    {
        Hide();
        QueueFree();
    }

    public override void _ExitTree()
    {
        // 取消窗口大小变化事件订阅
        ViewportResizeNotifier.Instance.OnViewportResized -= OnViewportResized;

        // 取消战斗事件订阅
        CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        ClearProviderContents();
    }

    private void OnCombatSetUp(CombatState _)
    {
        // 战斗开始时重新创建内容（重新订阅事件）
        MainFile.Logger.Debug("[PlayerOverlayPanel] CombatSetUp, rebuilding provider contents");
        RebuildProviderContents();
    }

    private void OnCombatEnded(CombatRoom _)
    {
        // 战斗结束时重新创建内容
        MainFile.Logger.Debug("[PlayerOverlayPanel] CombatEnded, rebuilding provider contents");
        RebuildProviderContents();
    }

    /// <summary>
    ///     创建并显示悬浮面板
    /// </summary>
    public static PlayerOverlayPanel Show(Player player, Vector2? position = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        var panel = new PlayerOverlayPanel
        {
            Name = $"PlayerOverlayPanel_{player.NetId}"
        };

        // 添加到场景树
        NRun.Instance?.GlobalUi.AddChild(panel);

        panel.Initialize(player);

        if (position.HasValue) panel.Position = position.Value;

        return panel;
    }
}
