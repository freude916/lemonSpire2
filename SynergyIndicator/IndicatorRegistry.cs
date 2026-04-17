using Godot;
using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace lemonSpire2.SynergyIndicator;

public static class IndicatorRegistry
{
    public enum IndicatorType
    {
        HandShake,
        Vulnerable,
        Weak,
        Strangle,
        Flanking,
        Strength,
        Block,
        Energy,
        Draw
    }

    private static readonly IReadOnlyDictionary<IndicatorType, Entry> Entries = CreateEntries();

    public static IReadOnlyList<IIndicatorProvider> Providers => [.. Entries.Values.Select(entry => entry.Provider)];

    public static Entry? GetEntry(IndicatorType type)
    {
        return Entries.GetValueOrDefault(type);
    }

    private static IReadOnlyDictionary<IndicatorType, Entry> CreateEntries()
    {
        var entries = new Dictionary<IndicatorType, Entry>();

        Register(entries, new HandShakeIndicatorProvider(), emoji: "🤝");
        Register(entries, new VulnerableIndicatorProvider(), PowerIcon<VulnerablePower>());
        Register(entries, new WeakIndicatorProvider(), PowerIcon<WeakPower>());
        Register(entries, new StrangleIndicatorProvider(), PowerIcon<StranglePower>());
        Register(entries, new FlankingIndicatorProvider(), PowerIcon<FlankingPower>());
        Register(entries, new StrengthIndicatorProvider(), PowerIcon<StrengthPower>());
        Register(entries, new BlockIndicatorProvider(), PowerIcon<BlockNextTurnPower>());
        Register(entries, new EnergyIndicatorProvider(), PowerIcon<EnergyNextTurnPower>());
        Register(entries, new DrawIndicatorProvider(), PowerIcon<DrawCardsNextTurnPower>());

        return entries;
    }

    private static void Register(
        IDictionary<IndicatorType, Entry> entries,
        IIndicatorProvider provider,
        Texture2D? icon = null,
        string? emoji = null)
    {
        entries[provider.Type] = new Entry(provider, icon, emoji);
    }

    private static Texture2D? PowerIcon<T>() where T : PowerModel
    {
        return ModelDb.AllPowers.FirstOrDefault(p => p is T)?.Icon;
    }

    public sealed record Entry(IIndicatorProvider Provider, Texture2D? Icon, string? Emoji);
}
