using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.BusinessObject
{
    public class Call
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public CallStatus Status { get; set; } = CallStatus.Queued;
        public string? ErrorMessage { get; set; }

        public double? DurationSeconds { get; set; }
        public int? ChannelCount { get; set; }
        public SpeakerMethod? SpeakerMethod { get; set; }
        public string? Language { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessingStartedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }

        public ICollection<CallPrompt> Prompts { get; set; } = new List<CallPrompt>();
        public ICollection<ProcessingEvent> Events { get; set; } = new List<ProcessingEvent>();

        public void AddEvent(string step, string? message = null, int? durationMs = null, bool isError = false)
        {
            Events.Add(new ProcessingEvent
            {
                Step = step,
                Message = message,
                DurationMs = durationMs,
                IsError = isError
            });
        }
    }
}
