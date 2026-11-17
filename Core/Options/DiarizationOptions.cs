using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Options
{
    public sealed class DiarizationOptions
    {
        public const string SectionName = "Diarization";

        // how many people speak in a call: 2 = agent and customer. 0 lets the model decide (extra speakers stay unlabelled).
        public int NumSpeakers { get; set; } = 2;

        // used for mono recordings, where nobody is known to be the agent:
        // true = whoever speaks first is the Agent, false = whoever speaks first is the Customer
        public bool AgentSpeaksFirst { get; set; } = true;
    }
}
