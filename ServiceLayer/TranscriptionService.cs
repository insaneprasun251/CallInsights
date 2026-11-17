using Core.BusinessObject;
using Core.DTO;
using Core.Exceptions;
using Core.Helpers;
using Core.Options;
using Core.Services;
using DataLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ServiceLayer
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly AppDbContext _db;
        private readonly StorageOptions _options;
        public TranscriptionService(AppDbContext db, IOptions<StorageOptions> options)
        {
            _db = db;
            _options = options.Value;
        }
        private string Folder => Path.GetFullPath(_options.TranscriptionsFolder);
        public async Task<List<TranscriptionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(Folder))
                return [];

            var result = new List<TranscriptionListItemDto>();
            foreach (var file in new DirectoryInfo(Folder).EnumerateFiles("*.json"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(file.Name), out var callId))
                    continue;

                try
                {
                    await using var stream = file.OpenRead();
                    var document = await JsonSerializer.DeserializeAsync<TranscriptionDocument>(
                        stream,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        },
                        cancellationToken);

                    if (document is null)
                        continue;

                    result.Add(new TranscriptionListItemDto
                    {
                        CallId = callId,
                        Language = document.Language,
                        DurationSeconds = document.DurationSeconds,
                        SegmentCount = document.Segments.Count,
                        ModifiedUtc = file.LastWriteTimeUtc,
                        SizeBytes = file.Length
                    });
                }
                catch (JsonException)
                {
                    // Skip malformed JSON files in the listing.
                }
                catch (IOException)
                {
                    // Skip files that cannot currently be read.
                }
            }

            var callIds = result.Select(x => x.CallId).ToList();
            var fileNamesByCallId = await _db.Calls.AsNoTracking().Where(c => callIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.FileName, cancellationToken);
            foreach (var item in result)
            {
                if (fileNamesByCallId.TryGetValue(item.CallId, out var fileName))
                {
                    item.RecordingFileName = fileName;
                }
            }
            return result.OrderByDescending(x => x.ModifiedUtc).ToList();
        }

        public Task<(Stream Content, string FileName)> DownloadAsync(Guid callId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = TranscriptionFileHelper.GetCallFilePath(Folder, callId);

            if (!File.Exists(path))
                throw new NotFoundException("Transcription file not found.");

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<(Stream Content, string FileName)>((stream, $"{callId}.json"));
        }
    }
}
