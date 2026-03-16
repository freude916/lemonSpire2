using Godot;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.PlayerColor;

/// <summary>
///     玩家颜色管理器
///     管理玩家的自定义颜色，支持网络同步
/// </summary>
public class ColorManager
{
    private static ColorManager? _instance;

    private readonly Dictionary<ulong, Color> _playerColors = new();

    private ColorManager()
    {
    }

    internal static Logger Log { get; } = new("lemon.color", LogType.GameSync);

    public static ColorManager Instance => _instance ??= new ColorManager();

    /// <summary>
    ///     玩家颜色变更事件
    /// </summary>
    public event Action<ulong, Color>? OnPlayerColorChanged;

    /// <summary>
    ///     获取玩家自定义颜色
    ///     如果没有自定义颜色，返回 null
    /// </summary>
    public Color? GetCustomColor(ulong playerId)
    {
        return _playerColors.TryGetValue(playerId, out var color) ? color : null;
    }

    /// <summary>
    ///     设置玩家颜色（本地）
    ///     触发 OnPlayerColorChanged 事件
    /// </summary>
    public void SetPlayerColor(ulong playerId, Color color)
    {
        _playerColors[playerId] = color;
        Log.Debug($"Player {playerId} color set to {color}");
        OnPlayerColorChanged?.Invoke(playerId, color);
    }

    /// <summary>
    ///     应用远程玩家颜色（从网络消息接收）
    ///     不广播，只更新本地状态
    /// </summary>
    internal void ApplyRemoteColor(ulong playerId, Color color)
    {
        _playerColors[playerId] = color;
        Log.Debug($"Player {playerId} color updated from network: {color}");
        OnPlayerColorChanged?.Invoke(playerId, color);
    }

    /// <summary>
    ///     清除玩家颜色（重置为默认）
    /// </summary>
    public void ClearPlayerColor(ulong playerId)
    {
        if (_playerColors.Remove(playerId))
            // 通知颜色已清除（返回默认）
            OnPlayerColorChanged?.Invoke(playerId, Colors.White);
    }

    /// <summary>
    ///     检查玩家是否有自定义颜色
    /// </summary>
    public bool HasCustomColor(ulong playerId)
    {
        return _playerColors.ContainsKey(playerId);
    }

    /// <summary>
    ///     重置所有颜色
    /// </summary>
    public void Reset()
    {
        _playerColors.Clear();
    }
}
