using Godot;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using DraggableTitleBar = lemonSpire2.util.Ui.DraggableTitleBar;
using ViewportResizeNotifier = lemonSpire2.util.Ui.ViewportResizeNotifier;

namespace lemonSpire2.PlayerStateEx.Panel;

/// <summary>
///     玩家悬浮面板
///     显示其他玩家的手牌、药水等信息
///     支持拖拽、Alt+Click 发送物品
/// </summary>
public partial class PlayerFloatingPanel : Control
{
    private readonly Dictionary<string, Control> _providerContents = new();
    private readonly List<Action> _unsubscribeActions = new();

    // 追踪上次 provider 可见状态
    private readonly HashSet<string> _visibleProviders = new();
    private VBoxContainer? _contentContainer;
    private DraggableTitleBar? _header;
    private Label? _headerTitle;
    private VBoxContainer? _mainContainer;

    // 脏标记：仅在事件触发时检查可见性变化
    private bool _needsVisibilityCheck;
    private PanelContainer? _panel;
    private Player? _player;

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
        CheckProviderVisibilityChanges();
    }

    private void CheckProviderVisibilityChanges()
    {
        if (_player == null) return;

        // 检查是否有新增的 provider
        var needsRebuild = false;
        foreach (var provider in PlayerPanelRegistry.GetProviders())
        {
            var shouldShow = provider.ShouldShow(_player);
            var wasVisible = _visibleProviders.Contains(provider.ProviderId);

            if (shouldShow != wasVisible)
            {
                needsRebuild = true;
                break;
            }
        }

        if (needsRebuild)
        {
            MainFile.Logger.Debug("[PlayerFloatingPanel] Provider visibility changed, rebuilding contents");
            RebuildProviderContents();
        }
    }

    private void CreateUi()
    {
        // 主面板
        _panel = new PanelContainer
        {
            Name = "Panel",
            AnchorsPreset = (int)LayoutPreset.TopLeft,
            CustomMinimumSize = new Vector2(280, 100)
        };

        // 添加样式
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f),
            BorderColor = new Color(0.4f, 0.4f, 0.5f),
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
            sectionTitle.AddThemeFontSizeOverride("font_size", 11);
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
    }

    private void RebuildProviderContents()
    {
        _panel!.Size = new Vector2(280, 100);
        _panel!.CustomMinimumSize = new Vector2(280, 100);

        // 重新构建所有内容
        CreateProviderContents();

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

        // 重置最小尺寸，让内容重新计算大小
        if (_panel != null)
        {
            _panel.Size = Vector2.Zero;
            _panel.CustomMinimumSize = Vector2.Zero;
        }

        if (_providerContents.TryGetValue(provider.ProviderId, out var content))
            provider.UpdateContent(_player, content);
    }

    private void ClearProviderContents()
    {
        // 取消事件订阅
        foreach (var unsubscribe in _unsubscribeActions) unsubscribe();
        _unsubscribeActions.Clear();

        // 清除 UI
        foreach (var content in _providerContents.Values) content.QueueFree();
        _providerContents.Clear();

        if (_contentContainer != null)
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
        MainFile.Logger.Debug("[PlayerFloatingPanel] CombatSetUp, rebuilding provider contents");
        RebuildProviderContents();
    }

    private void OnCombatEnded(CombatRoom _)
    {
        // 战斗结束时重新创建内容
        MainFile.Logger.Debug("[PlayerFloatingPanel] CombatEnded, rebuilding provider contents");
        RebuildProviderContents();
    }

    /// <summary>
    ///     创建并显示悬浮面板
    /// </summary>
    public static PlayerFloatingPanel Show(Player player, Vector2? position = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        var panel = new PlayerFloatingPanel
        {
            Name = $"PlayerFloatingPanel_{player.NetId}"
        };

        // 添加到场景树
        NRun.Instance?.GlobalUi.AddChild(panel);

        panel.Initialize(player);

        if (position.HasValue) panel.Position = position.Value;

        return panel;
    }
}
