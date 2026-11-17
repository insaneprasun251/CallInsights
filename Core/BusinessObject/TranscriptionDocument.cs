using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.BusinessObject
{
    public class TranscriptionDocument
    {
        public Guid CallId { get; set; }
        public string? Language { get; set; }
        public double? DurationSeconds { get; set; }
        public List<TranscriptionProviderResponse> ProviderResponses { get; set; } = [];

        // raw answer of the speaker identification server; only present for mono recordings
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement? Diarization { get; set; }

        public List<TranscriptionSegmentData> Segments { get; set; } = [];

        // the segments merged into speaker turns (consecutive lines by the same speaker)
        public List<SpeakerTurnData> Turns { get; set; } = [];
    }

    public class TranscriptionProviderResponse
    {
        public string SourceFile { get; set; } = string.Empty;
        public JsonElement Response { get; set; }
    }

    public class TranscriptionSegmentData
    {
        public int SequenceNo { get; set; }
        public string Speaker { get; set; } = "Unknown";
        public int StartMs { get; set; }
        public int EndMs { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class SpeakerTurnData
    {
        public int SequenceNo { get; set; }
        public string Speaker { get; set; } = "Unknown";
        public int StartMs { get; set; }
        public int EndMs { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    // one stretch of speech by one diarized speaker, as returned by the diarization server
    public class DiarizedTurn
    {
        public string Speaker { get; set; } = string.Empty;
        public int StartMs { get; set; }
        public int EndMs { get; set; }
    }

    public class SpeakerAssignmentSummary
    {
        public int DiarizedSpeakers { get; set; }
        public int UnlabelledLines { get; set; }
    }
}
