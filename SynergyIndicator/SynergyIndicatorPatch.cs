using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.SynergyIndicator;

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class SynergyIndicatorPatch
{
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

        __instance.Player.PlayerCombatState.Hand.CardAdded +=
            _ => IndicatorManager.UpdateSynergyStatus(__instance.Player);
        __instance.Player.PlayerCombatState.Hand.CardRemoved +=
            _ => IndicatorManager.UpdateSynergyStatus(__instance.Player);
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
}