using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace lemonSpire2.PlayerStateEx.RemoteFlash;

public static class RemoteUiFlashResolver
{
    public static CanvasItem? FindVisibleTarget(RemoteUiFlashMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message.Kind switch
        {
            RemoteUiFlashKind.HandCard => FindHandCardTarget(message.Card),
            RemoteUiFlashKind.Potion => FindPotionTarget(message.Potion),
            RemoteUiFlashKind.ShopCard => FindShopCardTarget(message.Card),
            RemoteUiFlashKind.ShopPotion => FindShopPotionTarget(message.Potion),
            RemoteUiFlashKind.ShopRelic => FindShopRelicTarget(message.Relic),
            RemoteUiFlashKind.CardReward => FindRewardCardTarget(message.Card),
            _ => null
        };
    }

    private static CanvasItem? FindHandCardTarget(SerializableCard? expected)
    {
        if (expected == null) return null;

        var me = GetLocalPlayer();
        var uiHand = NCombatRoom.Instance?.Ui?.Hand;
        var cards = me?.PlayerCombatState?.Hand.Cards;
        if (uiHand == null || cards == null) return null;

        var match = cards.FirstOrDefault(card =>
            RemoteUiFlashSnapshotMatcher.MatchesCard(expected, card.ToSerializable()));
        if (match == null) return null;

        var holder = uiHand.GetCardHolder(match);
        if (holder != null) return holder;
        return uiHand.GetCard(match);
    }

    private static NPotionHolder? FindPotionTarget(SerializablePotion? expected)
    {
        if (expected == null) return null;

        var container = NRun.Instance?.GlobalUi?.TopBar?.PotionContainer;
        if (container == null) return null;

        var holders = UiHelper.FindAll<NPotionHolder>(container)
            .Where(holder => holder.IsVisibleInTree() && holder.Potion?.Model != null)
            .ToArray();

        return holders.FirstOrDefault(holder =>
                   RemoteUiFlashSnapshotMatcher.MatchesPotionSlot(expected, CreatePotionSnapshot(holder.Potion!.Model)))
               ?? holders.FirstOrDefault(holder =>
                   RemoteUiFlashSnapshotMatcher.MatchesPotionId(expected, CreatePotionSnapshot(holder.Potion!.Model)));
    }

    private static NMerchantCard? FindShopCardTarget(SerializableCard? expected)
    {
        if (expected == null) return null;

        var inventory = NMerchantRoom.Instance?.Inventory;
        if (inventory == null || !inventory.IsVisibleInTree()) return null;

        return UiHelper.FindAll<NMerchantCard>(inventory)
            .FirstOrDefault(slot =>
                slot.IsVisibleInTree() &&
                slot.Entry is MerchantCardEntry { CreationResult.Card: { } card } &&
                RemoteUiFlashSnapshotMatcher.MatchesCard(expected, card.ToSerializable()));
    }

    private static NMerchantPotion? FindShopPotionTarget(SerializablePotion? expected)
    {
        if (expected == null) return null;

        var inventory = NMerchantRoom.Instance?.Inventory;
        if (inventory == null || !inventory.IsVisibleInTree()) return null;

        var slots = UiHelper.FindAll<NMerchantPotion>(inventory)
            .Where(slot => slot.IsVisibleInTree() && slot.Entry is MerchantPotionEntry { Model: not null })
            .ToArray();

        return slots.FirstOrDefault(slot =>
                   slot.Entry is MerchantPotionEntry { Model: { } potion } &&
                   RemoteUiFlashSnapshotMatcher.MatchesPotionSlot(expected, CreatePotionSnapshot(potion)))
               ?? slots.FirstOrDefault(slot =>
                   slot.Entry is MerchantPotionEntry { Model: { } potion } &&
                   RemoteUiFlashSnapshotMatcher.MatchesPotionId(expected, CreatePotionSnapshot(potion)));
    }

    private static NMerchantRelic? FindShopRelicTarget(SerializableRelic? expected)
    {
        if (expected == null) return null;

        var inventory = NMerchantRoom.Instance?.Inventory;
        if (inventory == null || !inventory.IsVisibleInTree()) return null;

        return UiHelper.FindAll<NMerchantRelic>(inventory)
            .FirstOrDefault(slot =>
                slot.IsVisibleInTree() &&
                slot.Entry is MerchantRelicEntry { Model: { } relic } &&
                RemoteUiFlashSnapshotMatcher.MatchesRelic(expected, relic.ToSerializable()));
    }

    private static NCardHolder? FindRewardCardTarget(SerializableCard? expected)
    {
        if (expected == null) return null;

        var globalUi = NRun.Instance?.GlobalUi;
        if (globalUi == null) return null;

        var roots = new Node?[] { globalUi.Overlays, globalUi.CapstoneContainer };
        foreach (var root in roots)
        {
            if (root == null) continue;

            var holder = UiHelper.FindAll<NCardHolder>(root)
                .FirstOrDefault(candidate =>
                    candidate.IsVisibleInTree() &&
                    candidate.CardModel != null &&
                    RemoteUiFlashSnapshotMatcher.MatchesCard(expected, candidate.CardModel.ToSerializable()));
            if (holder != null) return holder;
        }

        return null;
    }

    private static Player? GetLocalPlayer()
    {
        var state = RunManager.Instance.State;
        return state == null ? null : LocalContext.GetMe(state);
    }

    private static SerializablePotion CreatePotionSnapshot(PotionModel potion)
    {
        ArgumentNullException.ThrowIfNull(potion);

        var owner = potion.Owner;
        var slotIndex = owner?.GetPotionSlotIndex(potion) ?? -1;
        return new SerializablePotion
        {
            Id = potion.Id,
            SlotIndex = slotIndex
        };
    }
}
