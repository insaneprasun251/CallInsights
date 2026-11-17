using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IProcessingQueue
    {
        void Enqueue(Guid callId);
        IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
    }
}
