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
public class IndicatorManager
{
    private static IndicatorManager? _instance;

    private static readonly IReadOnlyList<IIndicatorProvider> Providers = new List<IIndicatorProvider>
    {
        new HandShakeIndicatorProvider(),
        new VulnerableIndicatorProvider(),
        new WeakIndicatorProvider(),
        new StrangleIndicatorProvider()
    };

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

    public void AddIndicator(ulong playerNetId, IndicatorType type, IndicatorStatus status)
    {
        if (!_panels.TryGetValue(playerNetId, out var panel)) return;
        panel.AddIndicator(type, status);
    }

    public void SetStatus(ulong playerNetId, IndicatorType type, IndicatorStatus status)
    {
        if (_panels.TryGetValue(playerNetId, out var panel)) panel.SetStatus(type, status);
    }

    public void ToggleStatus(ulong playerNetId, IndicatorType type)
    {
        if (!_panels.TryGetValue(playerNetId, out var panel)) return;
        panel.ToggleStatus(type);

        var newStatus = GetStatus(playerNetId, type);
        _networkHandler?.SendStatusMessage(playerNetId, type, newStatus);
    }

    public IndicatorStatus GetStatus(ulong playerNetId, IndicatorType type)
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

    public void FlashButton(ulong playerNetId, IndicatorType type)
    {
        if (_panels.TryGetValue(playerNetId, out var panel))
            panel.GetButton(type)?.PlayFlashAnimation();
    }

    public Dictionary<ulong, Dictionary<IndicatorType, IndicatorStatus>> GetAll()
    {
        var result = new Dictionary<ulong, Dictionary<IndicatorType, IndicatorStatus>>();
        foreach (var kvp in _panels) result[kvp.Key] = kvp.Value.GetAllStatuses();

        return result;
    }

    public static void UpdateSynergyStatus(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var netId = player.NetId;
        if (player.PlayerCombatState?.Hand.Cards == null) return;

        var cards = player.PlayerCombatState.Hand.Cards;
        var hasSynergy = cards.Any(c =>
            c.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly);

        Instance.ClearPlayerIndicators(netId);
        foreach (var provider in Providers)
            if (provider.ShouldShow(cards))
                Instance.AddIndicator(netId, provider.Type, IndicatorStatus.WillUse);

        MainFile.Logger.Debug(
            $"Updated synergy status for player {netId}: {(hasSynergy ? "Has synergy cards" : "No synergy cards")}");
    }
}
