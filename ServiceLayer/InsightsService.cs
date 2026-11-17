using Core.Constants;
using Core.DTO;
using Core.Exceptions;
using Core.Services;
using DataLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer
{
    public class InsightsService : IInsightsService
    {
        private readonly AppDbContext _db;
        public InsightsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<CallInsightDto>> GetByCallIdAsync(Guid callId, CancellationToken cancellationToken = default)
        {
            // Verify that the requested call exists.
            var callExists = await _db.Calls.AsNoTracking().AnyAsync(c => c.Id == callId, cancellationToken);

            if (!callExists)
                throw new NotFoundException(ErrorMessages.RecordingNotFound);

            // Read the saved LLM responses for this call.
            return await _db.CallPrompts.AsNoTracking().Where(p => p.CallId == callId).OrderBy(p => p.TemplateKey).ThenBy(p => p.TemplateVersion).Select(p => new CallInsightDto
                {
                    TemplateKey = p.TemplateKey,
                    TemplateVersion = p.TemplateVersion,
                    ResponseText = p.ResponseText,
                    ResponseModel = p.ResponseModel,
                    RespondedAtUtc = p.RespondedAtUtc
                }).ToListAsync(cancellationToken);
        }
    }
}
