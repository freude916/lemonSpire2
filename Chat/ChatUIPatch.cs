using HarmonyLib;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Chat;

/// <summary>
/// Harmony 补丁：在 NGlobalUi 初始化时注入聊天UI
/// </summary>
[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGlobalUi))]
public static class ChatUIPatch
{
    internal static readonly WeakNodeRegistry<ChatUi> _chatUIs = new();

    [HarmonyPatch("Initialize")]
    [HarmonyPostfix]
    public static void InitializePostfix(NGlobalUi __instance, RunState runState)
    {
        // 检查是否为多人游戏
        var netService = RunManager.Instance.NetService;
        if (netService == null || !netService.Type.IsMultiplayer())
        {
            return;
        }

        // 创建聊天UI
        var chatUI = new ChatUi();
        chatUI.Name = "ChatUI";

        // 添加到 NGlobalUi
        __instance.AddChild(chatUI);
        _chatUIs.Register(chatUI);

        // 初始化聊天管理器
        ChatManager.Instance.Initialize(netService);
        ChatManager.Instance.SetChatUI(chatUI);

        MainFile.Logger.Info("ChatUI injected into NGlobalUi");
    }
}

/// <summary>
/// Harmony 补丁：在游戏退出时清理聊天UI
/// </summary>
[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGame))]
public static class ChatUICleanupPatch
{
    [HarmonyPatch("_ExitTree")]
    [HarmonyPrefix]
    public static void ExitTreePrefix()
    {
        // 清理聊天管理器
        ChatManager.Instance.Cleanup();

        // 清理所有聊天UI实例
        ChatUIPatch._chatUIs.ForEachLive(ui => ui.QueueFree());
        MainFile.Logger.Debug("ChatUI instances cleaned up");
    }
}