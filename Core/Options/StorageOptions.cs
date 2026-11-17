using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Options
{
    public class StorageOptions
    {
        public const string SectionName = "Storage";
        public string RecordingsFolder { get; set; } = string.Empty;
        public string TranscriptionsFolder { get; set; } = string.Empty;
        public string[] AllowedExtensions { get; set; } = [".wav", ".mp3", ".m4a", ".ogg", ".opus", ".flac"];
        public int MaxFileSizeMb { get; set; } = 200;
    }
}
