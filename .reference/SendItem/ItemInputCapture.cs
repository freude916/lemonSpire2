using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Models;
using lemonSpire2.Chat.UI;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.SendItem;

/// <summary>
///     全局输入捕获节点 — 拦截 Alt+Click 发送物品链接
///     需要添加到场景树中才能工作
/// </summary>
public partial class ItemInputCapture : Control
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left, AltPressed: true })
        {
            return;
        }

        var hovered = GetViewport()?.GuiGetHoveredControl();
        if (hovered == null)
        {
            return;
        }

        // 排除聊天面板及其子节点
        if (IsInsideChatPanel(hovered))
        {
            return;
        }

        var segment = ItemInputHandler.FindItemSegment(hovered);
        if (segment == null)
        {
            return;
        }

        SendItemSegment(segment);
        GetViewport()?.SetInputAsHandled();
    }

    private static void SendItemSegment(ItemSegment segment)
    {
        var sync = ChatSynchronizer.Instance;
        if (sync == null)
        {
            return;
        }

        var senderId = RunManager.Instance.NetService.NetId;
        sync.Broadcast(senderId, new ChatSegment[] { segment });
    }

    private static bool IsInsideChatPanel(Control? control)
    {
        if (control == null)
        {
            return false;
        }

        var found = false;
        ChatUiPatch.ChatUIs.ForEachLive(chatUI =>
        {
            if (chatUI == control || chatUI.IsAncestorOf(control))
            {
                found = true;
            }
        });

        return found;
    }
}