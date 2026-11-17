using Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace ServiceLayer.Pipeline
{
    public class ProcessingQueue : IProcessingQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        public void Enqueue(Guid callId) => _channel.Writer.TryWrite(callId);

        public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
