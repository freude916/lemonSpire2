using System.Reflection;
using Godot;
using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.SynergyIndicator.Ui;

public class IndicatorClickedEventArgs : EventArgs
{
    public ulong PlayerNetId { get; init; }
    public IndicatorType IndicatorType { get; init; }
}

/// <summary>
///     指示器主面板容器，负责显示和管理所有玩家的指示器图标
/// </summary>
public partial class IndicatorPanel : HBoxContainer
{
    private static readonly FieldInfo? TopContainerField =
        typeof(NMultiplayerPlayerState).GetField("_topContainer", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly Dictionary<IndicatorType, IndicatorButton> _buttons = new();

    public IndicatorPanel()
    {
    }

    private IndicatorPanel(ulong playerNetId, bool isInteractive)
    {
        PlayerNetId = playerNetId;
        IsInteractive = isInteractive;
    }

    public ulong PlayerNetId { get; }

    public bool IsInteractive { get; }

    public event EventHandler<IndicatorClickedEventArgs>? IndicatorClicked;

    public static IndicatorPanel CreateForPlayer(NMultiplayerPlayerState player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var topContainer = TopContainerField?.GetValue(player) as HBoxContainer;

        if (topContainer == null) return null!;

        var isInteractive = LocalContext.IsMe(player.Player);
        var panel = new IndicatorPanel(player.Player.NetId, isInteractive);

        panel.ZIndex = 100;
        panel.MouseFilter = isInteractive
            ? MouseFilterEnum.Stop
            : MouseFilterEnum.Ignore;

        topContainer.AddChild(panel);
        panel.MoveToFront();
        MainFile.Logger.Debug($"CreateForPlayer: netId={player.Player.NetId} isInteractive={isInteractive}");

        var topContainerParent = topContainer.GetParent();
        if (topContainerParent != null)
            topContainerParent.MoveChild(topContainer, topContainerParent.GetChildCount() - 1);

        return panel;
    }

    /// <summary>
    ///     添加指定类型的指示器按钮
    /// </summary>
    public void AddIndicator(IndicatorType type, IndicatorStatus initialStatus = IndicatorStatus.WillUse)
    {
        if (_buttons.ContainsKey(type)) return;

        var button = new IndicatorButton();
        button.Setup(type, initialStatus, IsInteractive);
        button.MouseFilter = IsInteractive ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        AddChild(button);
        button.IndicatorClicked += OnIndicatorClicked;
        _buttons[type] = button;
    }

    /// <summary>
    ///     清空所有指示器按钮（回合开始时调用）
    /// </summary>
    public void Clear()
    {
        foreach (var kvp in _buttons)
        {
            var button = kvp.Value;
            RemoveChild(button);
            button.QueueFree();
        }

        _buttons.Clear();
    }

    public void ResetForNewTurn()
    {
        Clear();
    }

    /// <summary>
    ///     按钮点击事件：切换指示器状态
    /// </summary>
    public void SetStatus(IndicatorType type, IndicatorStatus status)
    {
        if (_buttons.TryGetValue(type, out var button)) button.SetStatus(status);
    }

    /// <summary>
    ///     切换指定指示器的状态（WillUse ⇄ WontUse）
    /// </summary>
    public void ToggleStatus(IndicatorType type)
    {
        if (!_buttons.TryGetValue(type, out var button)) return;
        var newStatus = button.Status == IndicatorStatus.WillUse
            ? IndicatorStatus.WontUse
            : IndicatorStatus.WillUse;
        button.SetStatus(newStatus);
    }

    /// <summary>
    ///     获取指定指示器的状态
    /// </summary>
    public IndicatorStatus GetStatus(IndicatorType type)
    {
        return _buttons.TryGetValue(type, out var button) ? button.Status : IndicatorStatus.WillUse;
    }

    /// <summary>
    ///     检查是否包含指定类型的指示器
    /// </summary>
    public bool HasIndicator(IndicatorType type)
    {
        return _buttons.ContainsKey(type);
    }

    public IndicatorButton? GetButton(IndicatorType type)
    {
        return _buttons.GetValueOrDefault(type);
    }

    /// <summary>
    ///     获取该面板所有指示器的状态
    /// </summary>
    public Dictionary<IndicatorType, IndicatorStatus> GetAllStatuses()
    {
        return _buttons.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Status);
    }

    private void OnIndicatorClicked(IndicatorType type)
    {
        MainFile.Logger.Debug(
            $"IndicatorClicked: panelNetId={PlayerNetId} type={type} localNetId={LocalContext.NetId}");
        IndicatorClicked?.Invoke(this, new IndicatorClickedEventArgs
        {
            PlayerNetId = PlayerNetId,
            IndicatorType = type
        });
    }
}
