using Core.Constants;
using Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace UI.Web.Api.Hubs
{
    public class SignalRRecordingNotifier : IRecordingNotifier
    {
        private readonly IHubContext<RecordingsHub> _hub;

        public SignalRRecordingNotifier(IHubContext<RecordingsHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyChangedAsync(CancellationToken cancellationToken = default) => _hub.Clients.All.SendAsync(HubDefinitions.RecordingsChangedEvent, cancellationToken);
    }
}
