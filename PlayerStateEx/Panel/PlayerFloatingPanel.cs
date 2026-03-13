using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

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
    private DraggableHeader? _header;
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

        if (needsRebuild) RebuildProviderContents();
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
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
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
        _header = new DraggableHeader
        {
            Name = "Header"
        };
        _header.SetDragTarget(_panel);
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
        // 重新构建所有内容
        CreateProviderContents();
    }

    private void UpdateProviderContent(IPlayerPanelProvider provider)
    {
        if (_player == null) return;

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
        ClearProviderContents();
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
