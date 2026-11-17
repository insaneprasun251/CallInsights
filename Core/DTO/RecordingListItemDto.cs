using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.DTO
{
    public class RecordingListItemDto
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime ModifiedUtc { get; set; }

        // null until the recording has been sent for processing
        public Guid? CallId { get; set; }
        public CallStatus? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
