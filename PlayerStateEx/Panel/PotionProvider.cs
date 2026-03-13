using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace lemonSpire2.PlayerStateEx.Panel;

/// <summary>
///     药水显示提供者
///     显示玩家的药水栏
///     支持 Alt+Click 发送药水到聊天
/// </summary>
public class PotionProvider : IPlayerPanelProvider
{
    private const float PotionScale = 0.6f;

    public string ProviderId => "potions";
    public int Priority => 20;
    public string DisplayName => "Potions";

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        // 只显示有药水的玩家
        // player.Potions.Any()
        return true; // 即使没有药水也显示，保持界面一致性
    }

    public Control CreateContent(Player player)
    {
        var container = new HBoxContainer
        {
            Name = "PotionsContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 4);

        // 不在这里调用 UpdateContent，等待加入场景树后再调用
        return container;
    }

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        if (content is not HBoxContainer container) return;

        // 清除现有内容
        foreach (var child in container.GetChildren()) child.QueueFree();

        MainFile.Logger.Info($"[PotionProvider] Updating content, player has {player.Potions.Count()} potions");

        foreach (var potion in player.Potions)
        {
            MainFile.Logger.Info($"[PotionProvider] Creating NPotion for {potion.Id.Entry}");

            var nPotion = NPotion.Create(potion);
            if (nPotion == null)
            {
                MainFile.Logger.Warn($"[PotionProvider] NPotion.Create returned null for {potion.Id.Entry}");
                continue;
            }

            var holder = NPotionHolder.Create(false);
            // 订阅点击事件，支持 Alt+Click 发送药水到聊天
            holder.Connect(NClickableControl.SignalName.Released,
                Callable.From(() => OnPotionHolderReleased(holder, potion)));

            holder.Scale = new Vector2(PotionScale, PotionScale);
            container.AddChild(holder);
            holder.AddPotion(nPotion);
            nPotion.Position = Vector2.Zero; // 关键：重置位置，否则会出现偏移

            MainFile.Logger.Info(
                $"[PotionProvider] Added potion {potion.Id.Entry} to holder, MouseFilter={holder.MouseFilter}");
        }
    }

    public Action? SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);
        MainFile.Logger.Debug($"[PotionProvider] SubscribeEvents for player with {player.Potions.Count()} potions");

        // 订阅药水变化事件
        // 事件类型是 Action<PotionModel>，需要适配
        void OnPotionChanged(PotionModel potion)
        {
            MainFile.Logger.Debug($"[PotionProvider] Potion event triggered for {potion?.Id.Entry ?? "null"}");
            onUpdate();
        }

        player.PotionProcured += OnPotionChanged;
        player.PotionDiscarded += OnPotionChanged;
        player.UsedPotionRemoved += OnPotionChanged;

        return () =>
        {
            player.PotionProcured -= OnPotionChanged;
            player.PotionDiscarded -= OnPotionChanged;
            player.UsedPotionRemoved -= OnPotionChanged;
        };
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        foreach (var child in content.GetChildren()) child?.QueueFree();
    }

    private static void OnPotionHolderReleased(NPotionHolder holder, PotionModel potion)
    {
        MainFile.Logger.Debug(
            $"[PotionProvider] PotionHolder released: {potion.Id.Entry}, Alt={Input.IsKeyPressed(Key.Alt)}");

        if (Input.IsKeyPressed(Key.Alt))
        {
            // Alt+Click: 发送药水到聊天
            var segment = new TooltipSegment
            {
                Tooltip = PotionTooltip.FromModel(potion),
                DisplayName = potion.HoverTip.Title ?? potion.Id.Entry
            };
            SendItemSegment(segment);
        }
        // 普通点击：不处理（holder 创建时 isUsable=false，不会打开使用弹窗）
    }

    private static void SendItemSegment(TooltipSegment segment)
    {
        var store = ChatStore.Instance;
        if (store == null)
        {
            MainFile.Logger.Warn("[PotionProvider] ChatStore.Instance is null");
            return;
        }

        store.Dispatch(new IntentSendSegments
        {
            receiverId = 0,
            Segments = [segment]
        });
        MainFile.Logger.Info($"[PotionProvider] Sent potion to chat: {segment.DisplayName}");
    }
}
