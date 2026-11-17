using Core.BusinessObject;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Core.Helpers
{
    public static class CallStatusExtensions
    {
        public static bool IsInProgress(this CallStatus status) =>
            status is CallStatus.Preprocessing
                or CallStatus.Transcribing
                or CallStatus.LabellingSpeakers
                or CallStatus.BuildingPrompt
                or CallStatus.GeneratingInsights;
    }

    public static class CallContextHelper
    {
        /// <summary>The call information and the conversation, one line per speaker turn, as the model will read it.</summary>
        public static string BuildContext(double? durationSeconds, SpeakerMethod? speakerMethod, IEnumerable<SpeakerTurnData> turns)
        {
            var text = new StringBuilder();

            if (durationSeconds is > 0)
                text.Append("Duration: ").AppendLine(FormatTime((int)Math.Round(durationSeconds.Value * 1000)));

            text.AppendLine(speakerMethod switch
            {
                SpeakerMethod.Channel => "Speakers: taken from the separate audio channels of the recording (reliable).",
                SpeakerMethod.Diarized => "Speakers: detected automatically from one mixed audio channel (can be wrong).",
                _ => "Speakers: not identified."
            });

            text.AppendLine();
            text.AppendLine("Conversation:");

            foreach (var turn in turns.Where(t => !string.IsNullOrWhiteSpace(t.Text)))
                text.Append('[').Append(FormatTime(turn.StartMs)).Append("] ").Append(turn.Speaker).Append(": ").AppendLine(turn.Text.Trim());

            return text.ToString().TrimEnd();
        }

        public static string BuildPrompt(string template, string context) => template.Replace("{context}", context);

        // mm:ss, or h:mm:ss for a call longer than one hour
        private static string FormatTime(int milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return time.TotalHours >= 1 ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}" : $"{time.Minutes:00}:{time.Seconds:00}";
        }
    }

    public static class FileNameHelper
    {
        /// <summary>Strips any folder part, so a client can never write outside the recordings folder.</summary>
        public static string GetSafeName(string? fileName) => Path.GetFileName(fileName ?? string.Empty).Trim();
        public static bool HasAllowedExtension(string fileName, IEnumerable<string> allowedExtensions) => allowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
    }

    public static class FileSizeHelper
    {
        private const long BytesPerMb = 1024L * 1024L;
        public static long MbToBytes(int megabytes) => megabytes * BytesPerMb;
    }

    public static class TextHelper
    {
        public static string Truncate(this string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
    }

    public static class WorkFolderHelper
    {
        public static string GetCallFolder(string workRoot, Guid callId) => Path.Combine(Path.GetFullPath(workRoot), callId.ToString());

        // best effort: a leftover work folder is harmless, so cleanup problems never fail a request
        public static void DeleteCallFolder(string workRoot, Guid callId)
        {
            var folder = GetCallFolder(workRoot, callId);
            try
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static class TranscriptionFileHelper
    {
        // the transcription files are written in camelCase; reading ignores the case
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static string GetCallFilePath(string transcriptionsRoot, Guid callId) => Path.Combine(Path.GetFullPath(transcriptionsRoot), $"{callId}.json");
        public static void DeleteCallFile(string transcriptionsRoot, Guid callId)
        {
            var file = GetCallFilePath(transcriptionsRoot, callId);
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static async Task<TranscriptionDocument?> ReadAsync(string path, CancellationToken cancellationToken)
        {
            if (!File.Exists(path))
                return null;

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            return await JsonSerializer.DeserializeAsync<TranscriptionDocument>(stream, JsonOptions, cancellationToken);
        }

        // written to a temporary file first, so a half-written transcription is never left behind
        public static async Task WriteAsync(string path, TranscriptionDocument document, CancellationToken cancellationToken)
        {
            var temporaryPath = path + ".tmp";

            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }

    public static class SpeakerHelper
    {
        // A transcript line further than this from every speaker turn gets no speaker.
        // Usually that is a Whisper hallucination inside a silent part of the call.
        public const int MaxGapMs = 1000;

        /// <summary>
        /// Gives every transcript line a speaker (Agent or Customer) from the diarized speaker turns.
        /// The speaker who talks first is the Agent when <paramref name="agentSpeaksFirst"/> is true, otherwise the Customer;
        /// the second speaker is the other one. A third speaker or later, and lines far from any turn, stay Unknown.
        /// </summary>
        public static SpeakerAssignmentSummary Assign(IList<TranscriptionSegmentData> segments, IReadOnlyList<DiarizedTurn> turns, bool agentSpeaksFirst)
        {
            // diarization labels (SPEAKER_00, SPEAKER_01 ...) carry no meaning, so they are ordered by who speaks first
            var speakerOrder = turns
                .GroupBy(t => t.Speaker)
                .Select(g => (Speaker: g.Key, First: g.Min(t => t.StartMs)))
                .OrderBy(x => x.First)
                .ThenBy(x => x.Speaker, StringComparer.Ordinal)
                .Select(x => x.Speaker)
                .ToList();

            var firstRole = agentSpeaksFirst ? SpeakerType.Agent : SpeakerType.Customer;
            var secondRole = agentSpeaksFirst ? SpeakerType.Customer : SpeakerType.Agent;
            var roles = new Dictionary<string, SpeakerType>();
            for (var i = 0; i < speakerOrder.Count; i++)
                roles[speakerOrder[i]] = i == 0 ? firstRole : i == 1 ? secondRole : SpeakerType.Unknown;

            var unlabelled = 0;
            foreach (var segment in segments)
            {
                var speaker = FindSpeaker(segment, turns, speakerOrder);
                var role = speaker is not null ? roles[speaker] : SpeakerType.Unknown;
                segment.Speaker = role.ToString();

                if (role == SpeakerType.Unknown)
                    unlabelled++;
            }

            return new SpeakerAssignmentSummary
            {
                DiarizedSpeakers = speakerOrder.Count,
                UnlabelledLines = unlabelled
            };
        }

        // the speaker who talks the longest during the line; a line between two turns goes to the nearest one
        private static string? FindSpeaker(TranscriptionSegmentData segment, IReadOnlyList<DiarizedTurn> turns, List<string> speakerOrder)
        {
            var overlapBySpeaker = new Dictionary<string, int>();
            string? nearest = null;
            var nearestGap = int.MaxValue;

            foreach (var turn in turns)
            {
                var overlap = Math.Min(segment.EndMs, turn.EndMs) - Math.Max(segment.StartMs, turn.StartMs);
                if (overlap > 0)
                {
                    overlapBySpeaker[turn.Speaker] = overlapBySpeaker.GetValueOrDefault(turn.Speaker) + overlap;
                    continue;
                }

                var gap = turn.EndMs < segment.StartMs ? segment.StartMs - turn.EndMs
                        : turn.StartMs > segment.EndMs ? turn.StartMs - segment.EndMs
                        : 0;
                if (gap < nearestGap)
                {
                    nearestGap = gap;
                    nearest = turn.Speaker;
                }
            }

            if (overlapBySpeaker.Count > 0)
                return overlapBySpeaker
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => speakerOrder.IndexOf(x.Key))
                    .First().Key;

            return nearestGap <= MaxGapMs ? nearest : null;
        }

        /// <summary>Merges consecutive lines of the same speaker into one turn.</summary>
        public static List<SpeakerTurnData> BuildTurns(IEnumerable<TranscriptionSegmentData> segments)
        {
            var turns = new List<SpeakerTurnData>();
            var text = new StringBuilder();

            foreach (var segment in segments.OrderBy(s => s.StartMs).ThenBy(s => s.SequenceNo))
            {
                var last = turns.Count > 0 ? turns[^1] : null;
                if (last is not null && last.Speaker == segment.Speaker)
                {
                    last.EndMs = Math.Max(last.EndMs, segment.EndMs);
                    text.Append(' ').Append(segment.Text);
                    continue;
                }

                if (last is not null)
                    last.Text = text.ToString();

                text.Clear().Append(segment.Text);
                turns.Add(new SpeakerTurnData
                {
                    SequenceNo = turns.Count + 1,
                    Speaker = segment.Speaker,
                    StartMs = segment.StartMs,
                    EndMs = segment.EndMs
                });
            }

            if (turns.Count > 0)
                turns[^1].Text = text.ToString();

            return turns;
        }
    }
}
