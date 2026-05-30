using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerStateEx;

[HarmonyPatch]
public static class AncientRelicChoicePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(EventModel), "SetEventState")]
    public static void SetEventStatePostfix(EventModel __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        var owner = __instance.Owner;
        if (owner == null)
            return;

        if (__instance is not AncientEventModel)
            return;

        var relics = __instance.CurrentOptions
            .Select(option => option.Relic)
            .OfType<RelicModel>()
            .ToList();

        if (relics.Count > 0)
            AncientRelicChoiceManager.Instance.UpdateChoices(owner.NetId, relics);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RunManager), "EnterRoom")]
    public static void EnterRoomPrefix()
    {
        AncientRelicChoiceManager.Instance.ClearAll();
    }
}
