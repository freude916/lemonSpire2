using IndicatorType = lemonSpire2.SynergyIndicator.IndicatorRegistry.IndicatorType;

namespace lemonSpire2.SynergyIndicator.Ui;

public class IndicatorClickedEventArgs : EventArgs
{
    public ulong PlayerNetId { get; init; }
    public IndicatorType IndicatorType { get; init; }
}
