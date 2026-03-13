using System.Reflection;
using Godot;
using HarmonyLib;
using lemonSpire2.PlayerStateEx.Panel;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerStateEx;

/// <summary>
///     Patch for NMultiplayerPlayerState to:
///     1. Show tooltips from registered providers on hover
///     2. Single click: Show floating panel
///     3. Double click / Right click: Show full expanded state
/// </summary>
[HarmonyPatchCategory("PlayerTooltip")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class NMultiplayerPlayerStatePatch
{
    /// <summary>
    ///     双击阈值（秒）
    /// </summary>
    private const double DoubleClickThreshold = 0.3;

    private static readonly FieldInfo? NetworkProblemIndicatorField =
        typeof(NMultiplayerPlayerState).GetField("_networkProblemIndicator",
            BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    ///     当前活跃的悬浮面板（按玩家 NetId 管理）
    /// </summary>
    private static readonly Dictionary<ulong, WeakReference<PlayerFloatingPanel>> ActivePanels = new();

    /// <summary>
    ///     当前活跃的 HoverTipSet（避免重复创建）
    /// </summary>
    private static readonly Dictionary<NMultiplayerPlayerState, WeakReference<NHoverTipSet>> ActiveTipSets = new();

    /// <summary>
    ///     上次点击时间（用于检测双击）
    /// </summary>
    private static readonly Dictionary<NMultiplayerPlayerState, double> LastClickTimes = [];

    #region Tooltip on hover

    [HarmonyPostfix]
    [HarmonyPatch("UpdateHighlightedState")]
    public static void UpdateHighlightedStatePostfix(NMultiplayerPlayerState __instance, bool ____isHighlighted)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        if (!____isHighlighted) return;

        var indicator = NetworkProblemIndicatorField?.GetValue(__instance) as NMultiplayerNetworkProblemIndicator;
        if (indicator != null && indicator.IsShown) return;

        ShowTooltips(__instance, __instance.Player);
    }

    private static void ShowTooltips(NMultiplayerPlayerState instance, Player player)
    {
        if (!PlayerTooltipRegistry.HasProviders) return;

        // 检查是否已存在 tooltip set，避免重复创建
        if (ActiveTipSets.TryGetValue(instance, out var weakRef) &&
            weakRef.TryGetTarget(out var existingSet) &&
            GodotObject.IsInstanceValid(existingSet))
            return; // 已存在，不重复创建

        var hoverTips = PlayerTooltipRegistry.GetHoverTips(player);

        var tipSet = NHoverTipSet.CreateAndShow(instance, hoverTips);
        tipSet.GlobalPosition = instance.GlobalPosition + Vector2.Down * 80f;

        ActiveTipSets[instance] = new WeakReference<NHoverTipSet>(tipSet);
    }

    #endregion

    #region Click handling - Single click / Double click

    [HarmonyPrefix]
    [HarmonyPatch("OnRelease")]
    public static bool OnReleasePrefix(NMultiplayerPlayerState __instance, NButton _)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        // 检查是否在目标选择模式
        var targetManager = NTargetManager.Instance;
        if (targetManager.IsInSelection)
        {
            LastClickTimes.Remove(__instance);
            return true; // 继续执行原始方法
        }

        var currentTime = Time.GetTicksMsec() / 1000.0;

        // 检查是否是双击
        if (LastClickTimes.TryGetValue(__instance, out var lastClickTime))
        {
            var timeSinceLastClick = currentTime - lastClickTime;
            LastClickTimes.Remove(__instance);

            if (timeSinceLastClick < DoubleClickThreshold)
            {
                // 双击：打开全屏详情
                OpenExpandedState(__instance);
                return false; // 阻止原始方法执行
            }
        }

        // 记录本次点击时间
        LastClickTimes[__instance] = currentTime;

        // 单击：显示悬浮面板
        ShowFloatingPanel(__instance);
        return false; // 阻止原始方法执行（不打开全屏状态）
    }

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NMultiplayerPlayerState __instance)
    {
        LastClickTimes.Remove(__instance);
        ActiveTipSets.Remove(__instance);
    }

    #endregion

    #region Right click handling

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        // 连接右键事件
        var hitbox = __instance.Hitbox;
        hitbox.GuiInput += eventArgs => OnHitboxGuiInput(__instance, eventArgs);
    }

    private static void OnHitboxGuiInput(NMultiplayerPlayerState instance, InputEvent @event)
    {
        // 右键点击：打开全屏状态
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            OpenExpandedState(instance);
            instance.GetViewport()?.SetInputAsHandled();
        }
    }

    #endregion

    #region Helper methods

    private static void ShowFloatingPanel(NMultiplayerPlayerState instance)
    {
        var player = instance.Player;
        if (player == null) return;

        var playerId = player.NetId;

        // 检查是否已存在该玩家的面板
        if (ActivePanels.TryGetValue(playerId, out var weakRef) &&
            weakRef.TryGetTarget(out var existingPanel) &&
            GodotObject.IsInstanceValid(existingPanel))
        {
            // 已存在，切换可见性
            if (existingPanel.Visible)
                existingPanel.Hide();
            else
                existingPanel.Show();
            return;
        }

        // 创建新面板
        var panelPos = instance.GlobalPosition + new Vector2(instance.Size.X + 10f, 0f);
        var panel = PlayerFloatingPanel.Show(player, panelPos);

        ActivePanels[playerId] = new WeakReference<PlayerFloatingPanel>(panel);
        var playerName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
        MainFile.Logger.Info($"Showing floating panel for player {playerName}");
    }

    private static void OpenExpandedState(NMultiplayerPlayerState instance)
    {
        var player = instance.Player;
        if (player == null) return;

        // 检查是否在目标选择模式
        var targetManager = NTargetManager.Instance;
        if (targetManager.IsInSelection ||
            targetManager.LastTargetingFinishedFrame == instance.GetTree().GetFrame()) return;

        var screen = NMultiplayerPlayerExpandedState.Create(player);
        NCapstoneContainer.Instance.Open(screen);
        var playerName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
        MainFile.Logger.Info($"Opening expanded state for player {playerName}");
    }

    #endregion
}
