using System;
using System.Collections.Generic;
using System.Text;

namespace Core.BusinessObject
{
    public class ProcessingEvent
    {
        public long Id { get; set; }
        public Guid CallId { get; set; }
        public string Step { get; set; } = string.Empty;
        public string? Message { get; set; }
        public int? DurationMs { get; set; }
        public bool IsError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Call? Call { get; set; }
    }
}
