using Core.BusinessObject;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Options;
using Core.Services;
using DataLayer;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Pipeline
{
    /// <summary>
    /// Builds the prompt for the language model from the speaker turns of the transcription,
    /// and saves it with the call. The model itself is not called here.
    /// </summary>
    public class PromptBuildStep : IPipelineStep
    {
        private readonly AppDbContext _db;
        private readonly StorageOptions _storage;

        public PromptBuildStep(AppDbContext db, IOptions<StorageOptions> storage)
        {
            _db = db;
            _storage = storage.Value;
        }

        public CallStatus Status => CallStatus.BuildingPrompt;
        public string Name => ProcessingSteps.BuildingPrompt;

        public async Task ExecuteAsync(Call call, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_storage.TranscriptionsFolder))
                throw new InvalidOperationException(ErrorMessages.SettingMissing("Storage:TranscriptionsFolder"));

            var path = TranscriptionFileHelper.GetCallFilePath(_storage.TranscriptionsFolder, call.Id);
            var document = await TranscriptionFileHelper.ReadAsync(path, cancellationToken)
                ?? throw new InvalidOperationException(ErrorMessages.TranscriptionNotFound);

            // processing the same call again replaces its prompts
            var oldPrompts = await _db.CallPrompts.Where(p => p.CallId == call.Id).ToListAsync(cancellationToken);
            _db.CallPrompts.RemoveRange(oldPrompts);

            var turns = (document.Turns.Count > 0 ? document.Turns : SpeakerHelper.BuildTurns(document.Segments))
                .Where(t => !string.IsNullOrWhiteSpace(t.Text))
                .ToList();

            if (turns.Count == 0)
            {
                call.AddEvent(ProcessingSteps.PromptDetails, "skipped (no transcript lines)");
                return;
            }

            var context = CallContextHelper.BuildContext(call.DurationSeconds, call.SpeakerMethod, turns);

            _db.CallPrompts.Add(new CallPrompt
            {
                Id = Guid.NewGuid(),
                CallId = call.Id,
                TemplateKey = PromptTemplates.CallInsightsKey,
                TemplateVersion = Defaults.TemplateVersion,
                ContextText = context,
                PromptText = CallContextHelper.BuildPrompt(PromptTemplates.CallInsights, context)
            });

            call.AddEvent(ProcessingSteps.PromptDetails, $"template={PromptTemplates.CallInsightsKey} {Defaults.TemplateVersion}; turns={turns.Count}; context={context.Length} chars");
        }
    }
}
