using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SyncReward;

/// <summary>
///     简单卡牌选择屏幕 Patch（CardGrid Reward）
///     Hook NSimpleCardSelectScreen 的两个 Create 重载，捕获事件/遗物等非战斗场景的卡牌奖励
///     - Create(List&lt;CardCreationResult&gt;, CardSelectorPrefs): FromSimpleGridForRewards 路径
///     - Create(List&lt;CardModel&gt;, CardSelectorPrefs): FromSimpleGrid 路径
/// </summary>
[HarmonyPatchCategory("CardRewardSync")]
[HarmonyPatch(typeof(NSimpleCardSelectScreen))]
public static class SimpleCardSelectScreenPatch
{
    private static Logger Log => CardRewardNetworkHandler.Log;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NSimpleCardSelectScreen.Create),
        typeof(IReadOnlyList<CardCreationResult>), typeof(CardSelectorPrefs))]
    public static void CreateFromRewardsPostfix(IReadOnlyList<CardCreationResult> cards)
    {
        if (cards == null || cards.Count == 0) return;

        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        var player = cards.Select(c => c.Card?.Owner).FirstOrDefault(o => o != null);
        if (player == null || !LocalContext.IsMe(player)) return;

        if (LocalContext.NetId == null)
        {
            Log.Warn("LocalContext.NetId is null, skipping card grid reward capture");
            return;
        }

        var cardModels = cards.Select(c => c.Card).Where(c => c != null).ToList();
        if (cardModels.Count == 0) return;

        var groupId = $"grid_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var group = new CardRewardGroup
        {
            GroupId = groupId,
            Source = CardRewardSourceType.CardGrid,
            Cards = [..cardModels.Select(CardEntry.FromModel)]
        };

        CardRewardSynchronizer.BroadcastCardReward(group);
        Log.Debug($"Captured CardGrid reward with {cardModels.Count} cards for player {player.NetId}");
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NSimpleCardSelectScreen.Create),
        typeof(IReadOnlyList<CardModel>), typeof(CardSelectorPrefs))]
    public static void CreateFromModelsPostfix(IReadOnlyList<CardModel> cards)
    {
        if (cards == null || cards.Count == 0) return;

        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        var player = cards.FirstOrDefault(o => o?.Owner != null)?.Owner;
        if (player == null || !LocalContext.IsMe(player)) return;

        if (LocalContext.NetId == null)
        {
            Log.Warn("LocalContext.NetId is null, skipping card grid reward capture");
            return;
        }

        var cardModels = cards.Where(c => c != null).ToList();
        if (cardModels.Count == 0) return;

        var groupId = $"grid_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var group = new CardRewardGroup
        {
            GroupId = groupId,
            Source = CardRewardSourceType.CardGrid,
            Cards = [..cardModels.Select(CardEntry.FromModel)]
        };

        CardRewardSynchronizer.BroadcastCardReward(group);
        Log.Debug($"Captured CardGrid reward (from models) with {cardModels.Count} cards for player {player.NetId}");
    }
}
