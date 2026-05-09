using Godot;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace lemonSpire2.Chat.Ui;

public sealed class EntityFocusManager
{
    private string? _currentMeta;
    private NCreature? _currentNode;

    public void RegisterHandlers(IntentHandlerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Register<IntentMetaHoverStart>(OnHoverStart);
        registry.Register<IntentMetaHoverEnd>(OnHoverEnd);
        registry.Register<IntentMetaClick>(OnClick);
    }

    private bool OnHoverStart(IntentMetaHoverStart intent)
    {
        if (!EntitySegment.IsEntityMeta(intent.Meta))
            return false;

        if (_currentMeta == intent.Meta && IsNodeAlive(_currentNode))
            return true;

        ClearFocus();

        if (!TryResolveCreature(intent.Meta, out var creature) || creature is null)
            return false;

        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node is null)
            return false;

        ApplyFocus(node);
        _currentMeta = intent.Meta;
        _currentNode = node;
        return true;
    }

    private bool OnHoverEnd(IntentMetaHoverEnd intent)
    {
        if (!EntitySegment.IsEntityMeta(intent.Meta))
            return false;

        ClearFocus();
        return true;
    }

    private bool OnClick(IntentMetaClick intent)
    {
        if (!EntitySegment.IsEntityMeta(intent.Meta))
            return false;

        ClearFocus();
        return true;
    }

    private static bool TryResolveCreature(string meta, out Creature? creature)
    {
        creature = null;
        if (!EntitySegment.TryParseMeta(meta, out var kind, out var playerNetId, out var creatureCombatId))
            return false;

        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state is null)
            return false;

        creature = kind switch
        {
            EntitySegment.EntityKind.Player => state.GetPlayer(playerNetId)?.Creature,
            EntitySegment.EntityKind.Creature => state.GetCreature(creatureCombatId),
            _ => null
        };

        return creature is not null;
    }

    private static void ApplyFocus(NCreature node)
    {
        node.ShowSingleSelectReticle();
        node.ShowHoverTips(node.Entity.HoverTips);
    }

    private void ClearFocus()
    {
        if (!IsNodeAlive(_currentNode))
        {
            _currentNode = null;
            _currentMeta = null;
            return;
        }

        _currentNode?.HideSingleSelectReticle();
        _currentNode?.HideHoverTips();
        _currentNode = null;
        _currentMeta = null;
    }

    private static bool IsNodeAlive(NCreature? node)
    {
        return node is not null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
