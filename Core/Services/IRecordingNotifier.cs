using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IRecordingNotifier
    {
        Task NotifyChangedAsync(CancellationToken cancellationToken = default);
    }
}
