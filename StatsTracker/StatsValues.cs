namespace lemonSpire2.StatsTracker;

/// <summary>
/// Dynamic statistics storage using a sorted dictionary.
/// Keys are i18n keys, values are float (displayed as integers).
/// </summary>
public class StatsValues
{
    private readonly SortedDictionary<string, float> _values = new();

    public void Add(string key, float amount)
    {
        if (_values.TryGetValue(key, out var existing))
        {
            _values[key] = existing + amount;
        }
        else
        {
            _values[key] = amount;
        }
    }

    public void Set(string key, float value) => _values[key] = value;

    public float Get(string key) => _values.GetValueOrDefault(key);

    public void Reset() => _values.Clear();

    public IEnumerable<KeyValuePair<string, float>> GetAll() => _values;

    public bool IsEmpty => _values.Count == 0;
}