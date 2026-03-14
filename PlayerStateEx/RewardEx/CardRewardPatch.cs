using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerStateEx.RewardEx;

/// <summary>
///     卡牌奖励网络初始化 Patch
///     在 NGlobalUi.Initialize 时初始化网络处理器
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.CommonUi.NGlobalUi), "Initialize")]
public static class CardRewardNetworkInitPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var netService = RunManager.Instance.NetService;

        CardRewardManager.Reset();

        if (netService.Type.IsMultiplayer())
        {
            CardRewardSynchronizer.Initialize(netService);
            MainFile.Logger.Info("[CardRewardPatch] CardRewardSynchronizer initialized for multiplayer");
        }
        else
        {
            MainFile.Logger.Info("[CardRewardPatch] Single player mode, CardRewardManager reset");
        }
    }
}

/// <summary>
///     奖励屏幕 Patch
///     在显示奖励时捕获卡牌奖励
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(NRewardsScreen))]
public static class RewardsScreenPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("SetRewards")]
    public static void SetRewardsPostfix(NRewardsScreen __instance, IEnumerable<Reward> rewards)
    {
        ArgumentNullException.ThrowIfNull(__instance);

        if (LocalContext.NetId == null)
        {
            MainFile.Logger.Warn("[CardRewardPatch] LocalContext.NetId is null, skipping card reward capture");
            return;
        }

        var playerNetId = LocalContext.NetId.Value;
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var groupIndex = 0;

        foreach (var reward in rewards)
        {
            switch (reward)
            {
                case MegaCrit.Sts2.Core.Rewards.CardReward cardReward:
                    ProcessCardReward(cardReward, playerNetId, timestamp, ref groupIndex);
                    break;
                case SpecialCardReward specialReward:
                    ProcessSpecialCardReward(specialReward, playerNetId, timestamp, ref groupIndex);
                    break;
            }
        }
    }

    private static void ProcessCardReward(MegaCrit.Sts2.Core.Rewards.CardReward cardReward, ulong playerNetId, string timestamp, ref int groupIndex)
    {
        var cards = cardReward.Cards.ToList();
        if (cards.Count == 0) return;

        var group = new CardRewardGroup
        {
            GroupId = $"{timestamp}_{groupIndex++}",
            Source = CardRewardSourceType.Normal,
            Cards = cards.Select(c => new CardEntry
            {
                ModelId = c.Id.Entry,
                UpgradeLevel = c.CurrentUpgradeLevel
            }).ToList()
        };

        CardRewardSynchronizer.BroadcastCardReward(group);
        MainFile.Logger.Debug($"[CardRewardPatch] Captured CardReward with {cards.Count} cards");
    }

    private static void ProcessSpecialCardReward(SpecialCardReward specialReward, ulong playerNetId, string timestamp, ref int groupIndex)
    {
        // 使用反射获取 _card 和 _customDescriptionEncounterSourceId
        var cardField = typeof(SpecialCardReward).GetField("_card",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var encounterSourceField = typeof(SpecialCardReward).GetField("_customDescriptionEncounterSourceId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var card = cardField?.GetValue(specialReward) as CardModel;
        if (card == null) return;

        var encounterSourceIdObj = encounterSourceField?.GetValue(specialReward);
        ModelId? encounterSourceId = encounterSourceIdObj != null ? (ModelId)encounterSourceIdObj : null;

        // 判断是否是盗牌归还
        var isStolenBack = IsStolenBackEncounter(encounterSourceId);

        var group = new CardRewardGroup
        {
            GroupId = $"{timestamp}_{groupIndex++}",
            Source = isStolenBack ? CardRewardSourceType.StolenBack : CardRewardSourceType.Special,
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
        MainFile.Logger.Debug($"[CardRewardPatch] Captured SpecialCardReward, isStolenBack={isStolenBack}");
    }

    /// <summary>
    ///     判断是否是盗牌归还的遭遇
    /// </summary>
    private static bool IsStolenBackEncounter(ModelId? encounterId)
    {
        if (encounterId == null || encounterId == ModelId.none) return false;

        // 盗牌相关的遭遇 ID
        var stolenBackEncounters = new HashSet<string>
        {
            "ThievingHopperWeak",
            "ThievingHopperStrong",
            // 可以添加其他盗牌相关的遭遇
        };

        return stolenBackEncounters.Contains(encounterId!.Entry);
    }
}

/// <summary>
///     房间切换 Patch
///     在进入新房间时清除所有人的卡牌奖励历史
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(RunManager))]
public static class RunManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("EnterRoom")]
    public static void EnterRoomPrefix()
    {
        // 进入新房间时清除所有人的历史
        CardRewardManager.Reset();
        MainFile.Logger.Debug("[CardRewardPatch] Cleared all card rewards on entering new room");
    }
}

/// <summary>
///     卡牌奖励同步静态入口
/// </summary>
public static class CardRewardSynchronizer
{
    private static CardRewardNetworkHandler? _handler;

    public static void Initialize(INetGameService netService)
    {
        _handler?.Dispose();
        _handler = new CardRewardNetworkHandler(netService);
    }

    public static void BroadcastCardReward(CardRewardGroup group)
    {
        _handler?.BroadcastCardReward(group);
    }

    public static void BroadcastClearRewards()
    {
        _handler?.BroadcastClearRewards();
    }

    public static void Dispose()
    {
        _handler?.Dispose();
        _handler = null;
    }
}
