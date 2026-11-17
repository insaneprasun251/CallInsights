using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Services;
using DataLayer;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ServiceLayer.Pipeline
{
    public class CallPipeline : ICallPipeline
    {
        private readonly AppDbContext _db;
        private readonly List<IPipelineStep> _steps;
        private readonly IRecordingNotifier _notifier;
        private readonly ILogger<CallPipeline> _logger;

        public CallPipeline(
            AppDbContext db,
            IEnumerable<IPipelineStep> steps,
            IRecordingNotifier notifier,
            ILogger<CallPipeline> logger)
        {
            _db = db;
            _steps = steps.OrderBy(s => (int)s.Status).ToList();
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Guid>> RecoverPendingAsync(CancellationToken cancellationToken = default)
        {
            var pending = await _db.Calls.Where(c => c.Status != CallStatus.Ready && c.Status != CallStatus.Failed).ToListAsync(cancellationToken);

            var changed = false;
            foreach (var call in pending.Where(c => c.Status != CallStatus.Queued))
            {
                call.Status = CallStatus.Queued;
                call.AddEvent(ProcessingSteps.Recovered, "Processing was interrupted and has been queued again");
                changed = true;
            }

            if (changed)
                await SaveAndNotifyAsync(cancellationToken);

            return pending.Select(c => c.Id).ToList();
        }

        public async Task RunAsync(Guid callId, CancellationToken cancellationToken = default)
        {
            var call = await _db.Calls.FirstOrDefaultAsync(c => c.Id == callId, cancellationToken);

            // guards against running the same call twice
            if (call is null || call.Status != CallStatus.Queued)
                return;

            call.ProcessingStartedAtUtc = DateTime.UtcNow;
            call.ProcessedAtUtc = null;
            call.ErrorMessage = null;
            call.AddEvent(ProcessingSteps.Started);

            foreach (var step in _steps)
            {
                call.Status = step.Status;
                await SaveAndNotifyAsync(cancellationToken);

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await step.ExecuteAsync(call, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw; // the app is stopping; RecoverPendingAsync queues this call again on the next start
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Step '{Step}' failed for call {CallId}", step.Name, callId);

                    call.Status = CallStatus.Failed;
                    call.ErrorMessage = ex.Message.Truncate(FieldLengths.ErrorMessage);
                    call.AddEvent(step.Name, ex.Message.Truncate(FieldLengths.EventMessage), (int)stopwatch.ElapsedMilliseconds, isError: true);
                    await SaveAndNotifyAsync(CancellationToken.None);
                    return;
                }

                call.AddEvent(step.Name, null, (int)stopwatch.ElapsedMilliseconds);
            }

            call.Status = CallStatus.Ready;
            call.ProcessedAtUtc = DateTime.UtcNow;
            call.AddEvent(ProcessingSteps.Completed);
            await SaveAndNotifyAsync(cancellationToken);
        }

        private async Task SaveAndNotifyAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyChangedAsync(cancellationToken);
        }
    }
}
