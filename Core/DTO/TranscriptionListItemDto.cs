using System;
using System.Collections.Generic;
using System.Text;

namespace Core.DTO
{
    public class TranscriptionListItemDto
    {
        public Guid CallId { get; set; }
        public string RecordingFileName { get; set; } = string.Empty;
        public string? Language { get; set; }
        public double? DurationSeconds { get; set; }
        public int SegmentCount { get; set; }
        public DateTime ModifiedUtc { get; set; }
        public long SizeBytes { get; set; }
    }
}
