using Core.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IInsightsService
    {
        Task<List<CallInsightDto>> GetByCallIdAsync(Guid callId, CancellationToken cancellationToken = default);
    }
}
