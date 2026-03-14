using System.Reflection;
using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace lemonSpire2.util.Ui;

/// <summary>
///     Provider 工具类
///     提供公共的工具方法，消除重复代码
/// </summary>
public static class ProviderUtils
{
    /// <summary>
    ///     清空 Control 的所有子节点，并重置容器大小
    /// </summary>
    public static void ClearChildren(Control container)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (var child in container.GetChildren())
        {
            // 先从父节点移除，再删除，避免布局计算时仍占用空间
            container.RemoveChild(child);
            child?.QueueFree();
        }
        // 重置容器大小
        container.Size = Vector2.Zero;
        container.CustomMinimumSize = Vector2.Zero;
    }

    /// <summary>
    ///     检测是否为 Alt+Click
    /// </summary>
    public static bool IsAltClick() => Input.IsKeyPressed(Key.Alt);

    /// <summary>
    ///     发送 TooltipSegment 到聊天
    /// </summary>
    public static void SendToChat(TooltipSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        var store = ChatStore.Instance;
        if (store == null)
        {
            MainFile.Logger.Warn("[ProviderUtils] ChatStore.Instance is null");
            return;
        }

        store.Dispatch(new IntentSendSegments
        {
            ReceiverId = 0,
            Segments = [segment]
        });
        MainFile.Logger.Info($"[ProviderUtils] Sent to chat: {segment.Tooltip.Render()}");
    }

    /// <summary>
    ///     设置 NPotionHolder 的缩放（封装反射）
    ///     需要在 AddPotion 之后调用
    /// </summary>
    public static void SetPotionScale(NPotionHolder holder, float scale)
    {
        var scaleField = typeof(NPotionHolder).GetField("_potionScale",
            BindingFlags.NonPublic | BindingFlags.Instance);
        scaleField?.SetValue(holder, Vector2.One * scale);
    }
}
