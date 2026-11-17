using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Enums
{
    public enum CallStatus
    {
        Queued = 0,
        Preprocessing = 1,
        Transcribing = 2,
        LabellingSpeakers = 3,
        BuildingPrompt = 4,
        Ready = 5,
        Failed = 6,
        GeneratingInsights = 7
    }

    public enum SpeakerMethod
    {
        Channel = 0,
        Diarized = 1
    }

    public enum SpeakerType
    {
        Unknown = 0,
        Agent = 1,
        Customer = 2
    }
}
