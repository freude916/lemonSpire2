using Godot;
using HarmonyLib;
using lemonSpire2.Chat.Ui;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.Chat;

/// <summary>
///     Harmony Patch, launch chat system when NGlobalUi (inside save) is initialized
/// </summary>
[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGlobalUi))]
public static class ChatUiPatch
{
    internal static readonly WeakNodeRegistry<Control> ChatUIs = new();

    internal static Logger Log { get; } = new("lemon.chat", LogType.Network);

    [HarmonyPatch("Initialize")]
    [HarmonyPostfix]
    public static void InitializePostfix(NGlobalUi __instance, RunState runState)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        InitializeChat(__instance, runState);
        Log.Info("Initialized");
    }

    private static void InitializeChat(NGlobalUi globalUi, RunState runState)
    {
        var netService = RunManager.Instance.NetService;

        var store = new ChatStore(netService);

        // Create ChatPanel with model, dispatch, intent registry, and tooltip parent (globalUi)
        var panel = new ChatPanel(store.Model, intent => store.Dispatch(intent), store.IntentRegistry,
            store.InputServices,
            globalUi);
        store.RegisterInputTextInserter(panel.InsertTextAtCaret);

        // Add to scene
        var control = panel.GetControl()!; // panel inited , control should be non-null
        globalUi.AddChild(control);
        ChatUIs.Register(control);

        panel.ResetPosition();
        Log.Info("Initialized");
    }
}
