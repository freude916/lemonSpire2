using Godot;
using MegaCrit.Sts2.Core.Entities.Players;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     玩家悬浮面板内容提供者接口
///     实现此接口以在 PlayerOverlayPanel 中添加自定义内容
/// </summary>
public interface IPlayerPanelProvider
{
    /// <summary>
    ///     提供者唯一标识
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     显示顺序优先级（数字越小越靠前）
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     显示名称（用于标题）
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     判断是否应该为该玩家显示此内容
    ///     判断为否时，CreateContent 和 UpdateContent 将不会被调用，且如果之前显示过内容也会被移除
    /// </summary>
    bool ShouldShow(Player player);

    /// <summary>
    ///     创建内容 UI 控件
    /// </summary>
    Control CreateContent(Player player);

    /// <summary>
    ///     更新内容（事件驱动时调用）
    /// </summary>
    void UpdateContent(Player player, Control content);

    /// <summary>
    ///     订阅玩家事件（返回取消订阅的 Action）
    /// </summary>
    Action? SubscribeEvents(Player player, Action onUpdate);

    /// <summary>
    ///     订阅影响可见性的事件（用于触发 ShouldShow 重新评估）
    ///     默认无订阅
    /// </summary>
    Action? SubscribeVisibilityEvents(Player player, Action onVisibilityChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onVisibilityChanged);
        return null;
    }

    /// <summary>
    ///     内容被移除时的清理
    /// </summary>
    void Cleanup(Control content);
}
