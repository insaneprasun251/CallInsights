using Core.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface ITranscriptionService
    {
        Task<List<TranscriptionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<(Stream Content, string FileName)> DownloadAsync(Guid callId, CancellationToken cancellationToken = default);
    }
}
