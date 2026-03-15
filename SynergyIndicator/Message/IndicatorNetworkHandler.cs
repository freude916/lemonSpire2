using lemonSpire2.SynergyIndicator.Models;
using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace lemonSpire2.SynergyIndicator.Message;

public class IndicatorNetworkHandler : NetworkHandlerBase<IndicatorStatusMessage>
{
    public IndicatorNetworkHandler(INetGameService netService) : base(netService)
    {
    }

    internal static Logger Log => SynergyIndicatorPatch.Log;

    public void SendStatusMessage(ulong playerNetId, IndicatorType type, IndicatorStatus status)
    {
        var message = new IndicatorStatusMessage
        {
            SenderId = playerNetId,
            IndicatorType = type,
            Status = status
        };
        SendMessage(message);
    }

    protected override void OnReceiveMessage(IndicatorStatusMessage message, ulong senderId)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (IsSelf(senderId)) return;

        Log.Debug(
            $"Received indicator status: player={message.SenderId} type={message.IndicatorType} status={message.Status}");
        IndicatorManager.Instance.SetStatus(message.SenderId, message.IndicatorType, message.Status);
    }
}
