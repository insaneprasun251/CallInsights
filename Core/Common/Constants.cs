using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Constants
{
    public static class DbTables
    {
        public const string Prefix = "ci_";
        public const string Calls = Prefix + "calls";
        public const string CallPrompts = Prefix + "call_prompts";
        public const string ProcessingEvents = Prefix + "processing_events";
        public const string MigrationsHistory = Prefix + "migrations_history";
    }

    public static class Defaults
    {
        public const string TemplateVersion = "v1";
    }

    public static class ErrorMessages
    {
        public const string NoFileUploaded = "No file was uploaded.";
        public const string RecordingNotFound = "Recording not found.";
        public const string FileNameOrPathTooLong = "The file name or path is too long.";
        public static string UnsupportedFileType(string extension) => $"File type '{extension}' is not supported.";
        public static string RecordingAlreadyExists(string fileName) => $"A recording named '{fileName}' already exists.";
        public static string FileTooLarge(int maxMb) => $"The file is larger than the allowed {maxMb} MB.";
        public const string RecordingInProgress = "This recording is being processed and can't be deleted right now.";
        public static string RecordingFileInUse(string fileName) => $"'{fileName}' could not be deleted because the file is in use.";
        public const string NoAudioStream = "The recording does not contain an audio stream.";
        public static string FfmpegNotFound(string path) => $"FFmpeg was not found at '{path}'. Check the Audio:FfmpegFolder setting.";
        public static string SettingMissing(string name) => $"The '{name}' setting is missing.";
        public const string NoAudioToTranscribe = "No prepared audio was found for this recording.";
        public const string TranscriptionApiKeyMissing = "The hosted transcription API key is not configured.";
        public const string EmptyAudioFile = "The prepared audio file is empty.";
        public const string TranscriptionServerRequestFailed = "The hosted transcription server request failed.";
        public const string InvalidTranscriptionResponse = "The hosted transcription server returned an invalid response.";
        public const string TranscriptionNotFound = "The transcription for this recording was not found.";
        public const string DiarizationServerRequestFailed = "The hosted speaker identification request failed.";
        public const string InvalidDiarizationResponse = "The hosted speaker identification returned an invalid response.";
        public const string LlmServerRequestFailed = "The hosted language model request failed.";
        public const string InvalidLlmResponse = "The hosted language model returned an invalid response.";
        public const string LlmAnswerCutOff = "The language model ran out of tokens before it finished its answer.";
    }

    public static class FieldLengths
    {
        public const int FileName = 255;
        public const int FilePath = 500;
        public const int ErrorMessage = 2000;
        public const int Language = 10;
        public const int TemplateKey = 50;
        public const int TemplateVersion = 20;
        public const int ModelName = 100;
        public const int EventStep = 50;
        public const int EventMessage = 1000;
    }

    public static class PromptTemplates
    {
        public const string CallInsightsKey = "call-insights";

        // {context} is replaced with the call context (see CallContextHelper.BuildContext)
        public const string CallInsights = """
            You analyse customer support phone calls between a support agent and a customer.

            The conversation below is an automatic transcription. It can contain mistakes: wrong or misspelled words, repeated lines, text in the wrong language or script, and made-up lines in silent parts of the call. The speaker labels were also detected automatically and can be wrong. Use only what the conversation clearly says and never invent details. When something is unclear, say so.

            Write your answer in English, even when the call is in Hindi or another language.

            Return only one JSON object, with no text before or after it and no markdown, using exactly these fields:
            {
              "summary": "two to four sentences about what the call was about and how it ended",
              "customerIssue": "the customer's problem or request, or unclear",
              "resolution": "what was done or agreed to solve it, or not resolved, or unclear",
              "customerSentiment": "one of: positive, neutral, negative, mixed, unclear",
              "actionItems": ["each follow-up action and who is responsible when it is said; an empty list when there are none"]
            }

            CALL
            {context}
            """;
    }

    public static class HubDefinitions
    {
        // under /api so the dev proxy and any future reverse proxy treat it like the rest of the API
        public const string RecordingsPath = "/api/hubs/recordings";
        public const string RecordingsChangedEvent = "recordingsChanged";
    }

    public static class ProcessingSteps
    {
        public const string Queued = "Queued";
        public const string Requeued = "Requeued";
        public const string Recovered = "Recovered";
        public const string Started = "Started";
        public const string Preprocessing = "Preparing audio";
        public const string Transcribing = "Transcribing";
        public const string TranscriptionDetails = "Transcription details";
        public const string LabellingSpeakers = "Identifying speakers";
        public const string SpeakerDetails = "Speaker details";
        public const string BuildingPrompt = "Building prompt";
        public const string PromptDetails = "Prompt details";
        public const string GeneratingInsights = "Generating insights";
        public const string InsightsDetails = "Insights details";
        public const string Completed = "Completed";
    }

    public static class WorkFileNames
    {
        // one channel (or a mono recording); speakers are separated later
        public const string Mono = "mono.wav";
        // stereo recording: one file per channel
        public const string Agent = "agent.wav";
        public const string Customer = "customer.wav";
    }
}
