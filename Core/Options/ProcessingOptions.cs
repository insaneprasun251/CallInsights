using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Options
{
    public class ProcessingOptions
    {
        public const string SectionName = "Processing";

        // how many recordings are processed at the same time
        public int MaxConcurrency { get; set; } = 1;
    }
}
