using Core.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IRecordingService
    {
        Task<List<RecordingListItemDto>> GetRecordingsAsync(CancellationToken cancellationToken = default);

        /// <summary>Saves an uploaded file into the recordings folder. Returns the stored file name.</summary>
        Task<string> SaveAsync(string originalFileName, Stream content, long sizeBytes, CancellationToken cancellationToken = default);

        /// <summary>Creates a Queued call for an existing recording (or re-queues a failed one). Returns the call id.</summary>
        Task<Guid> ProcessAsync(string fileName, CancellationToken cancellationToken = default);

        /// <summary>Deletes the recording file and its call data (transcript, prompts, events).</summary>
        Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
