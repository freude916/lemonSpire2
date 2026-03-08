using HarmonyLib;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Chat;

[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGlobalUi))]
public static class ChatUiPatch
{
    internal static readonly WeakNodeRegistry<ChatUi> _chatUIs = new();

    [HarmonyPatch("Initialize")]
    [HarmonyPostfix]
    public static void InitializePostfix(NGlobalUi __instance, RunState runState)
    {

        var netService = RunManager.Instance.NetService;
        if (netService == null || !netService.Type.IsMultiplayer())
        {
            return;
        }
        
        var chatUI = new ChatUi();
        chatUI.Name = "ChatUI";
        
        __instance.AddChild(chatUI);
        _chatUIs.Register(chatUI);
        
        ChatManager.Instance.Initialize(netService);
        ChatManager.Instance.SetChatUI(chatUI);

        MainFile.Logger.Info("ChatUI injected into NGlobalUi");
    }
}

[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGame), "_ExitTree")]
public static class ChatUICleanupPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        ChatManager.Instance.Cleanup();
        
        ChatUiPatch._chatUIs.ForEachLive(ui => ui.QueueFree());
        MainFile.Logger.Debug("ChatUI instances cleaned up");
    }
}