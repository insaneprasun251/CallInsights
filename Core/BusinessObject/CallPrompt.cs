using Core.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.BusinessObject
{
    public class CallPrompt
    {
        public Guid Id { get; set; }
        public Guid CallId { get; set; }
        public string TemplateKey { get; set; } = string.Empty;
        public string TemplateVersion { get; set; } = Defaults.TemplateVersion;
        public string ContextText { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? ResponseText { get; set; }
        public string? ResponseModel { get; set; }
        public DateTime? RespondedAtUtc { get; set; }
        public Call? Call { get; set; }
    }
}
