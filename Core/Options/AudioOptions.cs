using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Options
{
    public class AudioOptions
    {
        public const string SectionName = "Audio";

        // folder that contains ffmpeg.exe and ffprobe.exe
        public string FfmpegFolder { get; set; } = string.Empty;

        // converted audio is kept here, one subfolder per call
        public string WorkFolder { get; set; } = string.Empty;

        // the sample rate Whisper expects
        public int SampleRate { get; set; } = 16000;

        // in a stereo recording: 0 = the left channel is the agent, 1 = the right channel is the agent
        public int AgentChannelIndex { get; set; } = 0;
    }
}
