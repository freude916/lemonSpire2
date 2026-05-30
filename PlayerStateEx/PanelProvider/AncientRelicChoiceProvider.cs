using Godot;
using lemonSpire2.PlayerStateEx.RemoteFlash;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

public class AncientRelicChoiceProvider : IPlayerPanelProvider
{
    private const int ItemsPerRow = 3;
    private static Logger Log => PlayerPanelRegistry.Log;

    public string Id => "ancient_relic_choices";
    public int Priority => 16;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.ancientRelics").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return AncientRelicChoiceManager.Instance.HasChoices(player.NetId);
    }

    public Control CreateContent(Player player)
    {
        var container = new VBoxContainer
        {
            Name = "AncientRelicChoicesContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 8);
        return container;
    }

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (content is not VBoxContainer container) return;

        UiUtils.ClearChildren(container);

        var relics = AncientRelicChoiceManager.Instance.GetChoices(player.NetId);
        for (var i = 0; i < relics.Count; i += ItemsPerRow)
        {
            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            row.AddThemeConstantOverride("separation", 8);
            container.AddChild(row);

            for (var j = 0; j < ItemsPerRow && i + j < relics.Count; j++)
                AddRelicItem(row, player, relics[i + j]);
        }

        Log.Debug($"Updated Ancient relic choices for player {player.NetId}: {relics.Count} relics");
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);
        return SubscribeChoiceEvents(player, onUpdate);
    }

    public Action SubscribeVisibilityEvents(Player player, Action onVisibilityChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onVisibilityChanged);
        return SubscribeChoiceEvents(player, onVisibilityChanged);
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        UiUtils.ClearChildren(content);
    }

    private static Action SubscribeChoiceEvents(Player player, Action onUpdate)
    {
        void OnChoicesUpdated(ulong netId)
        {
            if (netId == player.NetId)
                onUpdate();
        }

        AncientRelicChoiceManager.Instance.ChoicesUpdated += OnChoicesUpdated;
        return () => AncientRelicChoiceManager.Instance.ChoicesUpdated -= OnChoicesUpdated;
    }

    private static void AddRelicItem(HBoxContainer row, Player player, RelicModel relic)
    {
        var holder = NRelicBasicHolder.Create(relic);
        if (holder == null) return;

        holder.GuiInput += @event => OnRelicGuiInput(holder, player, relic, @event);
        row.AddChild(holder);
    }

    private static void OnRelicGuiInput(Control clickedControl, Player player, RelicModel relic, InputEvent @event)
    {
        if (StsUtil.IsInSelection(clickedControl))
            return;

        switch (@event)
        {
            case InputEventMouseButton
            {
                Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right
            }:
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.AncientRelicChoice, relic);
                PlayerPanelChatHelper.SendRelicToChat(player, "LEMONSPIRE.chat.ancientRelicShare", relic);
                Log.Debug($"Sent Ancient relic choice to chat: {relic.Id.Entry}");
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.AncientRelicChoice, relic);
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                PlayerPanelChatHelper.OpenRelicDetails(relic);
                clickedControl.GetViewport()?.SetInputAsHandled();
                break;
        }
    }
}
