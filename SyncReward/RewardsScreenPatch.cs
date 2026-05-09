using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncReward;

/// <summary>
///     奖励屏幕 Patch
///     在显示奖励时捕获卡牌奖励
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(NRewardsScreen))]
public static class RewardsScreenPatch
{
    private static Logger Log => CardRewardNetworkHandler.Log;

    [HarmonyPostfix]
    [HarmonyPatch("ShowScreen")]
    public static void ShowScreenPostfix(RewardsSet set)
    {
        ArgumentNullException.ThrowIfNull(set);

        if (!LocalContext.IsMe(set.Player)) return;

        // Reward abstract class
        // ├─ CardReward
        // ├─ SpecialCardReward
        // ├─ GoldReward
        // ├─ PotionReward
        // ├─ RelicReward
        // ├─ CardRemovalReward
        // └─ LinkedRewardSet

        if (LocalContext.NetId == null)
        {
            Log.Warn("LocalContext.NetId is null, skipping card reward capture");
            return;
        }

        var rewards = set.Rewards;
        var rewardsSetId = set.Id >= 0 ? set.Id.ToString() : DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var groupIndex = 0;

        foreach (var reward in rewards)
            switch (reward)
            {
                case CardReward cardReward:
                    ProcessCardReward(cardReward, rewardsSetId, ref groupIndex);
                    break;
                case SpecialCardReward specialReward:
                    ProcessSpecialCardReward(specialReward, rewardsSetId, ref groupIndex);
                    break;
            }
    }

    private static void ProcessCardReward(CardReward cardReward, string rewardsSetId, ref int groupIndex)
    {
        var cards = cardReward.Cards.ToList();
        if (cards.Count == 0) return;

        var group = new CardRewardGroup
        {
            GroupId = $"{rewardsSetId}_{groupIndex++}",
            Source = CardRewardSourceType.Normal,
            Cards =
            [
                .. cards.Select(c => new CardEntry
                {
                    ModelId = c.Id.Entry,
                    UpgradeLevel = c.CurrentUpgradeLevel
                })
            ]
        };

        CardRewardSynchronizer.BroadcastCardReward(group);
        Log.Debug($"Captured CardReward with {cards.Count} cards");
    }

    private static void ProcessSpecialCardReward(SpecialCardReward specialReward, string rewardsSetId,
        ref int groupIndex)
    {
        // 使用反射获取 _card 和 _customDescriptionEncounterSourceId
        var cardField = typeof(SpecialCardReward).GetField("_card",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var encounterSourceField = typeof(SpecialCardReward).GetField("_customDescriptionEncounterSourceId",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var card = cardField?.GetValue(specialReward) as CardModel;
        if (card == null)
        {
            Log.Debug("SpecialCardReward._card is null, skipping");
            return;
        }

        var encounterSourceIdObj = encounterSourceField?.GetValue(specialReward);
        var encounterSourceId = encounterSourceIdObj != null ? (ModelId)encounterSourceIdObj : null;

        // 判断是否是盗牌归还
        var group = new CardRewardGroup
        {
            GroupId = $"{rewardsSetId}_{groupIndex++}",
            Source = CardRewardSourceType.Special,
            Cards =
            [
                new CardEntry
                {
                    ModelId = card.Id.Entry,
                    UpgradeLevel = card.CurrentUpgradeLevel
                }
            ]
        };

        CardRewardSynchronizer.BroadcastCardReward(group);
        Log.Debug($"Captured SpecialCardReward {card.Id.Entry}");
    }
}
