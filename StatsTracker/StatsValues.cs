namespace lemonSpire2.StatsTracker;

/// <summary>
///     Dynamic statistics storage using a sorted dictionary.
///     Keys are i18n keys, values are float (displayed as integers).
/// </summary>
public class StatsValues
{
    private readonly SortedDictionary<string, float> _values = new();

    public bool IsEmpty => _values.Count == 0;

    public void Add(string key, float amount)
    {
        if (_values.TryGetValue(key, out var existing))
            _values[key] = existing + amount;
        else
            _values[key] = amount;
    }

    public void Set(string key, float value)
    {
        _values[key] = value;
    }

    public float Get(string key)
    {
        return _values.GetValueOrDefault(key);
    }

    public void Reset()
    {
        _values.Clear();
    }

    /// <summary>
    ///     Reset only combat-specific stats, preserve total stats.
    /// </summary>
    public void ResetCombatStats()
    {
        var combatKeys = _values.Keys
            .Where(k => k.StartsWith("stats.combat.", StringComparison.Ordinal))
            .ToList();
        foreach (var key in combatKeys) _values.Remove(key);
    }

    public IEnumerable<KeyValuePair<string, float>> GetAll()
    {
        return _values;
    }
}
