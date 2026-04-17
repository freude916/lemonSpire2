using MegaCrit.Sts2.Core.Localization;

namespace lemonSpire2.Chat.Input.Command;

public static class ChatCmdText
{
    private const string Table = "gameplay_ui";

    public static string SystemHeader()
    {
        return Text("LEMONSPIRE.chat.command.system");
    }

    public static string AboutDescription()
    {
        return Text("LEMONSPIRE.chat.command.about.description");
    }

    public static string AboutBody()
    {
        return Text("LEMONSPIRE.chat.command.about.body");
    }

    public static string PingDescription()
    {
        return Text("LEMONSPIRE.chat.command.ping.description");
    }

    public static string PingUnavailable()
    {
        return Text("LEMONSPIRE.chat.command.ping.unavailable");
    }

    public static string PingPeer(string player, int pingMsec)
    {
        return Text("LEMONSPIRE.chat.command.ping.peer", ("Player", player), ("Ping", pingMsec.ToString()));
    }

    public static string PingClientHost(string pingMsec)
    {
        return Text("LEMONSPIRE.chat.command.ping.client_host", ("Ping", pingMsec));
    }

    public static string HelpDescription()
    {
        return Text("LEMONSPIRE.chat.command.help.description");
    }

    public static string WhisperDescription()
    {
        return Text("LEMONSPIRE.chat.command.whisper.description");
    }

    public static string HelpListEntry(string usage, string description)
    {
        return Text("LEMONSPIRE.chat.command.help.list_entry", ("Usage", usage), ("Description", description));
    }

    public static string CommandNameRequired()
    {
        return Text("LEMONSPIRE.chat.command.error.command_required");
    }

    public static string UnknownCommand(string command)
    {
        return Text("LEMONSPIRE.chat.command.error.unknown_command", ("Command", command));
    }

    public static string MissingArgument(string argument)
    {
        return Text("LEMONSPIRE.chat.command.error.missing_argument", ("Argument", argument));
    }

    public static string InvalidArgument(string argument)
    {
        return Text("LEMONSPIRE.chat.command.error.invalid_argument", ("Argument", argument));
    }

    public static string GreedyArgumentMustBeFinal(string argument)
    {
        return Text("LEMONSPIRE.chat.command.error.greedy_must_be_final", ("Argument", argument));
    }

    public static string TooManyArguments(string command)
    {
        return Text("LEMONSPIRE.chat.command.error.too_many_arguments", ("Command", command));
    }

    public static string PlayerArgumentMustUseMention()
    {
        return Text("LEMONSPIRE.chat.command.error.player_requires_mention");
    }

    public static string UnknownPlayer(string player)
    {
        return Text("LEMONSPIRE.chat.command.error.unknown_player", ("Player", player));
    }

    private static string Text(string entryKey, params (string Key, string Value)[] slots)
    {
        try
        {
            var loc = new LocString(Table, entryKey);
            foreach (var (key, value) in slots)
                loc.Add(key, value);

            var text = loc.GetFormattedText();
            if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, entryKey, StringComparison.Ordinal))
                return text;
        }
        catch (NullReferenceException)
        {
            // Unit tests run without the game's localization runtime; expose the raw key instead.
        }

        return entryKey;
    }
}
