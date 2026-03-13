using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace lemonSpire2.QoL;

/// <summary>
///     卡牌悬浮提示助手
///     为控件添加卡牌悬浮提示功能
/// </summary>
public static class CardHoverTipHelper
{
    /// <summary>
    ///     为控件绑定卡牌悬浮提示
    ///     使用方式：在控件的 MouseEntered/MouseExited 事件中调用
    /// </summary>
    /// <param name="control">要绑定提示的控件</param>
    /// <param name="card">卡牌模型</param>
    /// <param name="alignment">提示对齐方式</param>
    public static void ShowCardHoverTip(Control control, CardModel card,
        HoverTipAlignment alignment = HoverTipAlignment.None)
    {
        ArgumentNullException.ThrowIfNull(control);

        var hoverTip = new CardHoverTip(card);
        var tipSet = NHoverTipSet.CreateAndShow(control, hoverTip, alignment);

        // 设置位置偏移，让提示显示在控件下方
        tipSet.GlobalPosition = control.GlobalPosition + Vector2.Down * control.Size.Y;
    }

    /// <summary>
    ///     移除控件的悬浮提示
    /// </summary>
    /// <param name="control">要移除提示的控件</param>
    public static void HideCardHoverTip(Control control)
    {
        NHoverTipSet.Remove(control);
    }

    /// <summary>
    ///     为控件添加鼠标进入/离开时自动显示/隐藏卡牌悬浮提示的功能
    /// </summary>
    /// <param name="control">要绑定的控件</param>
    /// <param name="getCard">获取卡牌的函数</param>
    /// <param name="alignment">提示对齐方式</param>
    public static void BindCardHoverTip(Control control, Func<CardModel?> getCard,
        HoverTipAlignment alignment = HoverTipAlignment.None)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.Connect(Control.SignalName.MouseEntered, Callable.From(() =>
        {
            var card = getCard();
            if (card != null) ShowCardHoverTip(control, card, alignment);
        }));

        control.Connect(Control.SignalName.MouseExited, Callable.From(() => { HideCardHoverTip(control); }));
    }
}
