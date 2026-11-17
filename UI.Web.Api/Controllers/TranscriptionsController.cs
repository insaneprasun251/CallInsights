using Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace UI.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranscriptionsController : ControllerBase
    {
        private readonly ITranscriptionService _transcriptions;
        public TranscriptionsController(ITranscriptionService transcriptions)
        {
            _transcriptions = transcriptions;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _transcriptions.GetAllAsync(cancellationToken));
        }

        [HttpGet("{callId:guid}/download")]
        public async Task<IActionResult> Download(Guid callId, CancellationToken cancellationToken)
        {
            var (stream, fileName) = await _transcriptions.DownloadAsync(callId, cancellationToken);
            return File(stream, "application/json", fileName);
        }
    }
}
