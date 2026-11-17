using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface ICallPipeline
    {
        /// <summary>Finds calls that were queued or interrupted by a restart, makes them Queued, and returns their ids.</summary>
        Task<IReadOnlyList<Guid>> RecoverPendingAsync(CancellationToken cancellationToken = default);

        /// <summary>Runs all steps for one queued call.</summary>
        Task RunAsync(Guid callId, CancellationToken cancellationToken = default);
    }
}
