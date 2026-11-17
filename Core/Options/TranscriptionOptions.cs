using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Options
{
    public sealed class TranscriptionOptions
    {
        public const string SectionName = "Transcription";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 660;
        public string Language { get; set; } = "auto";
        public string Prompt { get; set; } = string.Empty;
    }
}
