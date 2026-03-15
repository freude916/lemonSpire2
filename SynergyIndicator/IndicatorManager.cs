using Godot;
using lemonSpire2.SynergyIndicator.Message;
using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using IndicatorPanel = lemonSpire2.SynergyIndicator.Ui.IndicatorPanel;

namespace lemonSpire2.SynergyIndicator;

/// <summary>
///     统一管理器，负责维护所有玩家的指示器 UI 面板
/// </summary>
public sealed class IndicatorManager : IDisposable
{
    private static IndicatorManager? _instance;

    private static readonly IReadOnlyList<IIndicatorProvider> Providers =
    [
        new HandShakeIndicatorProvider(),
        new VulnerableIndicatorProvider(),
        new WeakIndicatorProvider(),
        new StrangleIndicatorProvider()
    ];

    private readonly AudioStream? _noticeSound;

    /// <summary>
    ///     存储所有玩家的 UI 面板引用，使用 NetId 作为键
    /// </summary>
    private readonly Dictionary<ulong, IndicatorPanel> _panels = new();

    private IndicatorNetworkHandler? _networkHandler;

    private IndicatorManager()
    {
        _noticeSound = GD.Load<AudioStream>("res://lemonSpire2/synergy-notice.mp3");
    }

    public static IndicatorManager Instance => _instance ??= new IndicatorManager();

    public void InitializeNetwork(INetGameService netService)
    {
        _networkHandler = new IndicatorNetworkHandler(netService);
    }

    public void ResetAllIndicators()
    {
        foreach (var panel in _panels.Values)
            panel.Clear();
    }

    public void ClearPlayerIndicators(ulong playerNetId)
    {
        if (_panels.TryGetValue(playerNetId, out var panel)) panel.Clear();
    }

    private void AddIndicator(ulong playerNetId, IndicatorType type, IndicatorStatus status)
    {
        if (!_panels.TryGetValue(playerNetId, out var panel)) return;
        panel.AddIndicator(type, status);
    }

    public void SetStatus(ulong playerNetId, IndicatorType type, IndicatorStatus status)
    {
        if (_panels.TryGetValue(playerNetId, out var panel)) panel.SetStatus(type, status);
    }

    private void ToggleStatus(ulong playerNetId, IndicatorType type)
    {
        if (!_panels.TryGetValue(playerNetId, out var panel)) return;
        panel.ToggleStatus(type);

        var newStatus = GetStatus(playerNetId, type);
        _networkHandler?.SendStatusMessage(playerNetId, type, newStatus);
    }

    private IndicatorStatus GetStatus(ulong playerNetId, IndicatorType type)
    {
        return _panels.TryGetValue(playerNetId, out var panel)
            ? panel.GetStatus(type)
            : IndicatorStatus.WillUse;
    }

    public IndicatorPanel? CreatePanel(NMultiplayerPlayerState player)
    {
        var panel = IndicatorPanel.CreateForPlayer(player);

        _panels[panel.PlayerNetId] = panel;
        panel.TreeExited += () => _panels.Remove(panel.PlayerNetId);
        panel.IndicatorClicked += (_, args) => ToggleStatus(args.PlayerNetId, args.IndicatorType);
        return panel;
    }

    public bool HasIndicator(ulong playerNetId, IndicatorType type)
    {
        return _panels.TryGetValue(playerNetId, out var panel) && panel.HasIndicator(type);
    }

    public void PlayNoticeSound()
    {
        if (_noticeSound == null) return;

        var parent = _panels.Values.FirstOrDefault();
        if (parent == null) return;

        var audioPlayer = new AudioStreamPlayer
        {
            Stream = _noticeSound,
            VolumeDb = -3f // 对应原作者的音量设置
        };
        parent.AddChild(audioPlayer);
        audioPlayer.Play();
        audioPlayer.Finished += audioPlayer.QueueFree; // 播放完自动销毁
    }

    public Dictionary<ulong, Dictionary<IndicatorType, IndicatorStatus>> GetAll()
    {
        var result = new Dictionary<ulong, Dictionary<IndicatorType, IndicatorStatus>>();
        foreach (var kvp in _panels) result[kvp.Key] = kvp.Value.GetAllStatuses();

        return result;
    }

    /// <summary>
    ///     差异更新玩家指示器：只添加新出现的，移除不再需要的
    /// </summary>
    public static void UpdateSynergyStatus(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var netId = player.NetId;
        if (player.PlayerCombatState?.Hand.Cards == null) return;

        var shouldShowTypes = PlayerExpectedTypes(player.PlayerCombatState);

        var currentTypes = PlayerCurrentTypes(player);

        UpdateIndicator(shouldShowTypes, currentTypes, netId);
    }

    /// <summary>
    ///     移除指定玩家的特定指示器
    /// </summary>
    private void RemoveIndicator(ulong playerNetId, IndicatorType type)
    {
        if (_panels.TryGetValue(playerNetId, out var panel))
            panel.RemoveIndicator(type);
    }

    private static HashSet<IndicatorType> PlayerExpectedTypes(PlayerCombatState state)
    {
        var cards = state.Hand.Cards;
        var expectedTypes = new HashSet<IndicatorType>();
        foreach (var provider in Providers)
            if (provider.ShouldShow(cards))
                expectedTypes.Add(provider.Type);
        return expectedTypes;
    }

    private static HashSet<IndicatorType> PlayerCurrentTypes(Player player)
    {
        return Instance._panels.TryGetValue(player.NetId, out var panel)
            ? [.. panel.GetIndicatorTypes()]
            : [];
    }

    private static void UpdateIndicator(HashSet<IndicatorType> shouldShowTypes, HashSet<IndicatorType> currentTypes,
        ulong netId)
    {
        foreach (var type in shouldShowTypes.Where(type => !currentTypes.Contains(type)))
        {
            Instance.AddIndicator(netId, type, IndicatorStatus.WillUse);
        }

        foreach (var type in currentTypes.Where(type => !shouldShowTypes.Contains(type)))
        {
            Instance.RemoveIndicator(netId, type);
        }
    }

    public void Dispose()
    {
        _noticeSound?.Dispose();
        _networkHandler?.Dispose();
    }
}