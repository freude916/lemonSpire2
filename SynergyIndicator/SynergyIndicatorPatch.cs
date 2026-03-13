using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.SynergyIndicator;

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class SynergyIndicatorPatch
{
    /// <summary>
    ///     存储每个玩家实例的事件处理程序，用于在 _ExitTree 时取消订阅
    /// </summary>
    private static readonly
        Dictionary<NMultiplayerPlayerState, (Action<CardModel> CardAdded, Action<CardModel> CardRemoved)>
        _eventHandlers =
            new();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        IndicatorManager.Instance.CreatePanel(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnCombatSetUp")]
    public static void OnCombatSetUpPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        IndicatorManager.UpdateSynergyStatus(__instance.Player);

        if (!LocalContext.IsMe(__instance.Player)) return;

        if (__instance.Player.PlayerCombatState == null) return;

        // 创建事件处理程序并保存引用，以便后续取消订阅
        Action<CardModel> cardAddedHandler = _ => IndicatorManager.UpdateSynergyStatus(__instance.Player);
        Action<CardModel> cardRemovedHandler = _ => IndicatorManager.UpdateSynergyStatus(__instance.Player);

        __instance.Player.PlayerCombatState.Hand.CardAdded += cardAddedHandler;
        __instance.Player.PlayerCombatState.Hand.CardRemoved += cardRemovedHandler;

        _eventHandlers[__instance] = (cardAddedHandler, cardRemovedHandler);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnCardAdded")]
    public static void OnCardAddedPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        IndicatorManager.UpdateSynergyStatus(__instance.Player);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnCardRemoved")]
    public static void OnCardRemovedPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        IndicatorManager.UpdateSynergyStatus(__instance.Player);
    }

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        // 取消订阅事件并清理引用
        if (_eventHandlers.Remove(__instance, out var handlers))
            if (__instance.Player?.PlayerCombatState?.Hand != null)
            {
                __instance.Player.PlayerCombatState.Hand.CardAdded -= handlers.CardAdded;
                __instance.Player.PlayerCombatState.Hand.CardRemoved -= handlers.CardRemoved;
            }
    }
}

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class SynergyIndicatorNetworkPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGlobalUi __instance, RunState runState)
    {
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        IndicatorManager.Instance.InitializeNetwork(netService);
        MainFile.Logger.Info("IndicatorManager network initialized");
    }
}
