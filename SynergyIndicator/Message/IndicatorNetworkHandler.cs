using lemonSpire2.SynergyIndicator.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace lemonSpire2.SynergyIndicator.Message;

public class IndicatorNetworkHandler
{
    private readonly INetGameService _netService;

    public IndicatorNetworkHandler(INetGameService netService)
    {
        _netService = netService;
        _netService.RegisterMessageHandler<IndicatorStatusMessage>(OnReceiveStatusMessage);
    }

    public void SendStatusMessage(ulong playerNetId, IndicatorType type, IndicatorStatus status)
    {
        var message = new IndicatorStatusMessage
        {
            SenderId = playerNetId,
            IndicatorType = type,
            Status = status
        };
        _netService.SendMessage(message);
    }

    private void OnReceiveStatusMessage(IndicatorStatusMessage message, ulong senderId)
    {
        if (senderId == _netService.NetId) return;

        MainFile.Logger.Debug(
            $"Received indicator status: player={message.SenderId} type={message.IndicatorType} status={message.Status}");
        IndicatorManager.Instance.SetStatus(message.SenderId, message.IndicatorType, message.Status);
    }
}
