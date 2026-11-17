using System;
using System.Collections.Generic;
using System.Text;

namespace Core.DTO
{
    public sealed class CallInsightDto
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string TemplateVersion { get; set; } = string.Empty;
        public string? ResponseText { get; set; }
        public string? ResponseModel { get; set; }
        public DateTime? RespondedAtUtc { get; set; }
    }
}
