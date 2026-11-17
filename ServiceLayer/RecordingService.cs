using Core.BusinessObject;
using Core.Constants;
using Core.DTO;
using Core.Enums;
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

namespace ServiceLayer
{
    public class RecordingService : IRecordingService
    {
        private readonly AppDbContext _db;
        private readonly StorageOptions _options;
        private readonly IRecordingNotifier _notifier;
        private readonly IProcessingQueue _queue;
        private readonly AudioOptions _audio;

        public RecordingService(AppDbContext db, IOptions<StorageOptions> options, IOptions<AudioOptions> audio, IRecordingNotifier notifier, IProcessingQueue queue)
        {
            _db = db;
            _options = options.Value;
            _audio = audio.Value;
            _notifier = notifier;
            _queue = queue;
        }

        private string Folder => Path.GetFullPath(_options.RecordingsFolder);

        public async Task<List<RecordingListItemDto>> GetRecordingsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(Folder))
                return new List<RecordingListItemDto>();

            var calls = await _db.Calls
                .AsNoTracking()
                .Select(c => new { c.Id, c.FilePath, c.Status, c.ErrorMessage })
                .ToListAsync(cancellationToken);

            var callsByPath = calls.ToDictionary(c => c.FilePath, StringComparer.OrdinalIgnoreCase);

            var result = new List<RecordingListItemDto>();
            foreach (var file in new DirectoryInfo(Folder).EnumerateFiles())
            {
                if (!FileNameHelper.HasAllowedExtension(file.Name, _options.AllowedExtensions))
                    continue;

                callsByPath.TryGetValue(file.FullName, out var call);

                result.Add(new RecordingListItemDto
                {
                    FileName = file.Name,
                    SizeBytes = file.Length,
                    ModifiedUtc = file.LastWriteTimeUtc,
                    CallId = call?.Id,
                    Status = call?.Status,
                    ErrorMessage = call?.ErrorMessage
                });
            }

            return result.OrderByDescending(r => r.ModifiedUtc).ToList();
        }

        public async Task<string> SaveAsync(string originalFileName, Stream content, long sizeBytes, CancellationToken cancellationToken = default)
        {
            var safeName = FileNameHelper.GetSafeName(originalFileName);

            if (!FileNameHelper.HasAllowedExtension(safeName, _options.AllowedExtensions))
                throw new BadRequestException(ErrorMessages.UnsupportedFileType(Path.GetExtension(safeName)));

            if (sizeBytes <= 0)
                throw new BadRequestException(ErrorMessages.NoFileUploaded);

            if (sizeBytes > FileSizeHelper.MbToBytes(_options.MaxFileSizeMb))
                throw new BadRequestException(ErrorMessages.FileTooLarge(_options.MaxFileSizeMb));

            var fullPath = Path.Combine(Folder, safeName);

            if (safeName.Length > FieldLengths.FileName || fullPath.Length > FieldLengths.FilePath)
                throw new BadRequestException(ErrorMessages.FileNameOrPathTooLong);

            Directory.CreateDirectory(Folder);

            if (File.Exists(fullPath))
                throw new ConflictException(ErrorMessages.RecordingAlreadyExists(safeName));

            FileStream target;
            try
            {
                // CreateNew fails if the file appeared in the meantime, so two uploads can't overwrite each other
                target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
            }
            catch (IOException) when (File.Exists(fullPath))
            {
                throw new ConflictException(ErrorMessages.RecordingAlreadyExists(safeName));
            }

            try
            {
                await using (target)
                {
                    await content.CopyToAsync(target, cancellationToken);
                }
            }
            catch
            {
                File.Delete(fullPath); // don't leave a half-written file behind
                throw;
            }

            await _notifier.NotifyChangedAsync(cancellationToken);
            return safeName;
        }

        public async Task<Guid> ProcessAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var safeName = FileNameHelper.GetSafeName(fileName);
            var fullPath = Path.Combine(Folder, safeName);

            if (!FileNameHelper.HasAllowedExtension(safeName, _options.AllowedExtensions) || !File.Exists(fullPath))
                throw new NotFoundException(ErrorMessages.RecordingNotFound);

            if (safeName.Length > FieldLengths.FileName || fullPath.Length > FieldLengths.FilePath)
                throw new BadRequestException(ErrorMessages.FileNameOrPathTooLong);

            var existing = await _db.Calls.FirstOrDefaultAsync(c => c.FilePath == fullPath, cancellationToken);
            if (existing is not null)
            {
                // a failed call can be retried; anything else is already in progress or done
                if (existing.Status == CallStatus.Failed)
                {
                    TranscriptionFileHelper.DeleteCallFile(_options.TranscriptionsFolder, existing.Id);
                    existing.Status = CallStatus.Queued;
                    existing.ErrorMessage = null;
                    existing.ProcessingStartedAtUtc = null;
                    existing.ProcessedAtUtc = null;
                    existing.AddEvent(ProcessingSteps.Requeued, "Processing requested again");
                    await _db.SaveChangesAsync(cancellationToken);
                    await _notifier.NotifyChangedAsync(cancellationToken);
                    _queue.Enqueue(existing.Id);
                }

                return existing.Id;
            }

            var call = new Call
            {
                Id = Guid.NewGuid(),
                FileName = safeName,
                FilePath = fullPath,
                Status = CallStatus.Queued
            };
            call.AddEvent(ProcessingSteps.Queued, "Processing requested");

            _db.Calls.Add(call);
            await _db.SaveChangesAsync(cancellationToken);
            await _notifier.NotifyChangedAsync(cancellationToken);
            _queue.Enqueue(call.Id);

            return call.Id;
        }

        public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var safeName = FileNameHelper.GetSafeName(fileName);
            var fullPath = Path.Combine(Folder, safeName);

            // only recordings can be deleted, never other files that happen to be in the folder
            if (!FileNameHelper.HasAllowedExtension(safeName, _options.AllowedExtensions))
                throw new NotFoundException(ErrorMessages.RecordingNotFound);

            var call = await _db.Calls.FirstOrDefaultAsync(c => c.FilePath == fullPath, cancellationToken);
            var fileExists = File.Exists(fullPath);

            if (call is null && !fileExists)
                throw new NotFoundException(ErrorMessages.RecordingNotFound);

            if (call is not null && call.Status.IsInProgress())
                throw new ConflictException(ErrorMessages.RecordingInProgress);

            // database first: a leftover file is harmless (it shows up as "Not processed"),
            // a call row without its file is not
            if (call is not null)
            {
                _db.Calls.Remove(call);
                await _db.SaveChangesAsync(cancellationToken);
                WorkFolderHelper.DeleteCallFolder(_audio.WorkFolder, call.Id);
                TranscriptionFileHelper.DeleteCallFile(_options.TranscriptionsFolder, call.Id);
            }

            if (fileExists)
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (IOException)
                {
                    await _notifier.NotifyChangedAsync(cancellationToken); // the call data is already gone
                    throw new ConflictException(ErrorMessages.RecordingFileInUse(safeName));
                }
            }

            await _notifier.NotifyChangedAsync(cancellationToken);
        }
    }
}
