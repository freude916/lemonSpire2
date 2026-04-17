namespace lemonSpire2.util;

/// <summary>
///     优先级注册表
///     按优先级排序管理注册项
/// </summary>
/// <typeparam name="T">注册项类型</typeparam>
public class PriorityRegistry<T> where T : class
{
    private readonly List<T> _items = new();

    /// <summary>
    ///     已注册项（按优先级排序）
    /// </summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>
    ///     是否有注册项
    /// </summary>
    public bool HasItems => _items.Count > 0;

    /// <summary>
    ///     注册项
    /// </summary>
    /// <param name="item">要注册的项</param>
    /// <param name="getPriority">获取优先级的函数</param>
    /// <param name="getId">可选：获取唯一标识的函数，用于去重</param>
    public void Register(T item, Func<T, int> getPriority, Func<T, string?>? getId = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(getPriority);

        if (getId != null)
        {
            var id = getId(item);
            if (id != null && _items.Any(i => getId(i) == id))
                throw new InvalidOperationException($"Duplicate registry id '{id}'.");
        }
        else if (_items.Contains(item))
        {
            throw new InvalidOperationException($"Duplicate registry item '{item}'.");
        }

        _items.Add(item);
        _items.Sort((a, b) => getPriority(a).CompareTo(getPriority(b)));
    }

    /// <summary>
    ///     注销项
    /// </summary>
    public bool Unregister(T item)
    {
        return _items.Remove(item);
    }

    /// <summary>
    ///     通过 ID 注销项
    /// </summary>
    public void UnregisterById(Func<T, string?> getId, string id)
    {
        _items.RemoveAll(i => getId(i) == id);
    }

    /// <summary>
    ///     清空所有注册
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }
}
