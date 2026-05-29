using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.PlayerStateEx;

public sealed class AncientRelicChoiceManager
{
    private readonly ConcurrentDictionary<ulong, Collection<RelicModel>> _playerChoices = new();

    private AncientRelicChoiceManager()
    {
    }

    public static AncientRelicChoiceManager Instance { get; } = new();

    public event Action<ulong>? ChoicesUpdated;

    public void UpdateChoices(ulong playerNetId, IEnumerable<RelicModel> relics)
    {
        ArgumentNullException.ThrowIfNull(relics);

        var snapshots = new Collection<RelicModel>(
            [.. relics.Select(relic => (RelicModel)relic.MutableClone())]);

        if (snapshots.Count == 0)
        {
            ClearChoices(playerNetId);
            return;
        }

        _playerChoices[playerNetId] = snapshots;
        ChoicesUpdated?.Invoke(playerNetId);
    }

    public void ClearChoices(ulong playerNetId)
    {
        var removed = _playerChoices.TryRemove(playerNetId, out _);
        if (removed)
            ChoicesUpdated?.Invoke(playerNetId);
    }

    public Collection<RelicModel> GetChoices(ulong playerNetId)
    {
        if (!_playerChoices.TryGetValue(playerNetId, out var choices))
            return [];

        return new Collection<RelicModel>(
            [.. choices.Select(relic => (RelicModel)relic.MutableClone())]);
    }

    public bool HasChoices(ulong playerNetId)
    {
        return _playerChoices.TryGetValue(playerNetId, out var choices) && choices.Count > 0;
    }

    public void ClearAll()
    {
        if (_playerChoices.IsEmpty)
            return;

        var netIds = _playerChoices.Keys.ToArray();
        _playerChoices.Clear();
        foreach (var netId in netIds)
            ChoicesUpdated?.Invoke(netId);
    }
}
