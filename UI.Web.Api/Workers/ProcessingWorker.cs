using Core.Options;
using Core.Services;
using Microsoft.Extensions.Options;

namespace UI.Web.Api.Workers
{
    public class ProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IProcessingQueue _queue;
        private readonly ProcessingOptions _options;
        private readonly ILogger<ProcessingWorker> _logger;

        public ProcessingWorker(
            IServiceScopeFactory scopes,
            IProcessingQueue queue,
            IOptions<ProcessingOptions> options,
            ILogger<ProcessingWorker> logger)
        {
            _scopes = scopes;
            _queue = queue;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RecoverAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Could not recover pending calls");
            }

            var consumers = Enumerable
                .Range(0, Math.Max(1, _options.MaxConcurrency))
                .Select(_ => ConsumeAsync(stoppingToken));

            await Task.WhenAll(consumers);
        }

        private async Task RecoverAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopes.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<ICallPipeline>();

            foreach (var id in await pipeline.RecoverPendingAsync(cancellationToken))
                _queue.Enqueue(id);
        }

        private async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var callId in _queue.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var pipeline = scope.ServiceProvider.GetRequiredService<ICallPipeline>();
                        await pipeline.RunAsync(callId, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Unexpected error while processing call {CallId}", callId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // the app is shutting down
            }
        }
    }
}
