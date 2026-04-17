namespace lemonSpire2.Chat.Input.Command;

public sealed class ChatCmdRegistry
{
    private readonly Dictionary<string, ChatCmdSpec> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ChatCmdSpec> All => _commands.Values
        .OrderBy(static command => command.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public void Register(ChatCmdSpec command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_commands.ContainsKey(command.Name))
            throw new InvalidOperationException($"Duplicate chat command '{command.Name}'.");
        _commands[command.Name] = command;
    }

    public bool TryGet(string name, out ChatCmdSpec? command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _commands.TryGetValue(name, out command);
    }
}
