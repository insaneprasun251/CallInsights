using Core.BusinessObject;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IPipelineStep
    {
        /// <summary>The status a call has while this step runs. The enum order is also the step order.</summary>
        CallStatus Status { get; }

        /// <summary>The name written to the progress log.</summary>
        string Name { get; }

        Task ExecuteAsync(Call call, CancellationToken cancellationToken);
    }
}
