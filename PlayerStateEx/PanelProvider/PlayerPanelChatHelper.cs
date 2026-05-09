using lemonSpire2.Chat;
using lemonSpire2.Chat.Message;
using lemonSpire2.PlayerStateEx.RemoteFlash;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

internal static class PlayerPanelChatHelper
{
    public static void SendCardToChat(Player player, string locEntryKey, CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        SendPlayerItemToChat(player, locEntryKey, new TooltipSegment
        {
            Tooltip = CardTooltip.FromModel(card)
        });
    }

    public static void SendPotionToChat(Player player, string locEntryKey, PotionModel potion)
    {
        ArgumentNullException.ThrowIfNull(potion);

        SendPlayerItemToChat(player, locEntryKey, new TooltipSegment
        {
            Tooltip = PotionTooltip.FromModel(potion)
        });
    }

    public static void SendRelicToChat(Player player, string locEntryKey, RelicModel relic)
    {
        ArgumentNullException.ThrowIfNull(relic);

        SendPlayerItemToChat(player, locEntryKey, new TooltipSegment
        {
            Tooltip = RelicTooltip.FromModel(relic)
        });
    }

    public static void SendPlayerItemToChat(Player player, string locEntryKey, TooltipSegment tooltipSegment)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(locEntryKey);
        ArgumentNullException.ThrowIfNull(tooltipSegment);

        ChatStore.SendToChat(
            new TemplateSegment
            {
                Template = new LocString("gameplay_ui", locEntryKey),
                Slots =
                [
                    EntitySegment.FromPlayer(player).ToNamedSegment("Player"),
                    tooltipSegment.ToNamedSegment("Item")
                ]
            }
        );
    }

    public static void OpenHandCardDetails(Player player, CardModel card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        var cards = player.PlayerCombatState?.Hand.Cards.ToList() ?? [];
        var index = cards.IndexOf(card);
        if (index >= 0)
            NGame.Instance?.GetInspectCardScreen().Open(cards, index);
    }

    public static void OpenCardDetails(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        NGame.Instance?.GetInspectCardScreen().Open([card], 0);
    }

    public static void OpenRelicDetails(RelicModel relic)
    {
        ArgumentNullException.ThrowIfNull(relic);
        NGame.Instance?.GetInspectRelicScreen().Open([relic], relic);
    }

    public static void RequestRemoteFlash(Player player, RemoteUiFlashKind kind, CardModel card)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(card);

        var snapshotSource = card.IsMutable ? card : (CardModel)card.MutableClone();
        RemoteUiFlashSynchronizer.Send(new RemoteUiFlashMessage
        {
            SenderId = RunManager.Instance.NetService.NetId,
            TargetPlayerId = player.NetId,
            Kind = kind,
            Card = snapshotSource.ToSerializable()
        });
    }

    public static void RequestRemoteFlash(Player player, RemoteUiFlashKind kind, PotionModel potion)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(potion);

        var slotIndex = player.GetPotionSlotIndex(potion);
        if (slotIndex < 0 && potion.Owner != null)
            slotIndex = potion.Owner.GetPotionSlotIndex(potion);

        var snapshot = new SerializablePotion
        {
            Id = potion.Id,
            SlotIndex = Math.Max(0, slotIndex)
        };

        RemoteUiFlashSynchronizer.Send(new RemoteUiFlashMessage
        {
            SenderId = RunManager.Instance.NetService.NetId,
            TargetPlayerId = player.NetId,
            Kind = kind,
            Potion = snapshot
        });
    }

    public static void RequestRemoteFlash(Player player, RemoteUiFlashKind kind, RelicModel relic)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(relic);

        var snapshotSource = relic.IsMutable ? relic : (RelicModel)relic.MutableClone();
        RemoteUiFlashSynchronizer.Send(new RemoteUiFlashMessage
        {
            SenderId = RunManager.Instance.NetService.NetId,
            TargetPlayerId = player.NetId,
            Kind = kind,
            Relic = snapshotSource.ToSerializable()
        });
    }
}
