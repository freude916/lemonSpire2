using Godot;
using HarmonyLib;
using lemonSpire2.Chat.Ui;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Chat;

/// <summary>
///     Harmony Patch, launch chat system when NGlobalUi (inside save) is initialized
/// </summary>
[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGlobalUi))]
public static class ChatUiPatch
{
    internal static readonly WeakNodeRegistry<Control> ChatUIs = new();

    [HarmonyPatch("Initialize")]
    [HarmonyPostfix]
    public static void InitializePostfix(NGlobalUi __instance, RunState runState)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        InitializeChat(__instance, runState);
    }

    private static void InitializeChat(NGlobalUi globalUi, RunState runState)
    {
        var netService = RunManager.Instance.NetService;

        var store = new ChatStore(netService);

        // Create ChatPanel with model, dispatch, intent registry, and tooltip parent (globalUi)
        var panel = new ChatPanel(store.Model, intent => store.Dispatch(intent), store.IntentRegistry, globalUi);

        // Add to scene
        var control = panel.GetControl()!; // panel inited , control should be non-null
        globalUi.AddChild(control);
        ChatUIs.Register(control);

        panel.ResetPosition();

        MainFile.Logger.Info("Chat system initialized successfully");
    }
}

[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGame), "_ExitTree")]
public static class ChatUiCleanupPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        ChatStore.Instance = null;
        ChatUiPatch.ChatUIs.ForEachLive(ui => ui.QueueFree());

        MainFile.Logger.Debug("ChatUI instances cleaned up");
    }
}
