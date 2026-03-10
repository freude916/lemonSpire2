using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;

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
        MainFile.Logger.Info("ItemInputCapture ready");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left, AltPressed: true })
            return;

        MainFile.Logger.Debug("Alt+Click detected");

        var hovered = GetViewport()?.GuiGetHoveredControl();
        if (hovered == null)
        {
            MainFile.Logger.Debug("No hovered control");
            return;
        }

        MainFile.Logger.Debug($"Hovered: {hovered.Name} ({hovered.GetType().Name})");

        // 排除聊天面板及其子节点
        if (IsInsideChatPanel(hovered))
        {
            MainFile.Logger.Debug("Inside chat panel, ignoring");
            return;
        }

        var segment = ItemInputHandler.FindItemSegment(hovered);
        if (segment == null)
        {
            MainFile.Logger.Debug("No item segment found");
            return;
        }

        MainFile.Logger.Info($"Found item: {segment.DisplayName}");
        SendItemSegment(segment);
        GetViewport()?.SetInputAsHandled();
    }

    private static void SendItemSegment(TooltipSegment segment)
    {
        var store = ChatStore.Instance;
        if (store == null)
        {
            MainFile.Logger.Warn("ChatStore.Instance is null");
            return;
        }

        var msg = new ChatMessage
        {
            SenderId = 0, // Will be filled by ChatStore
            Segments = new List<IMsgSegment> { segment }
        };

        store.Dispatch(new IntentSubmit { Message = msg });
    }

    private static bool IsInsideChatPanel(Control? control)
    {
        if (control == null)
            return false;

        var found = false;
        ChatUiPatch.ChatUIs.ForEachLive(chatUI =>
        {
            if (chatUI == control || chatUI.IsAncestorOf(control))
                found = true;
        });

        return found;
    }
}
