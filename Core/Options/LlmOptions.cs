using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Options
{
    public sealed class LlmOptions
    {
        public const string SectionName = "Llm";

        // 0 gives the most predictable answers, higher values give more varied ones.
        // A low value is right for pulling facts out of a call.
        public double Temperature { get; set; } = 0.2;

        // The model writes out its reasoning before the answer, and that counts against this limit too,
        // so it has to stay high. The hosted server accepts at most 4096.
        public int MaxTokens { get; set; } = 4096;
    }
}
