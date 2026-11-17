using Core.Constants;
using Core.Exceptions;
using Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace UI.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordingsController : ControllerBase
    {
        private readonly IRecordingService _recordings;

        public RecordingsController(IRecordingService recordings)
        {
            _recordings = recordings;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _recordings.GetRecordingsAsync(cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                throw new BadRequestException(ErrorMessages.NoFileUploaded);

            await using var stream = file.OpenReadStream();
            var storedName = await _recordings.SaveAsync(file.FileName, stream, file.Length, cancellationToken);

            return Ok(new { fileName = storedName });
        }

        [HttpPost("{fileName}/process")]
        public async Task<IActionResult> Process(string fileName, CancellationToken cancellationToken)
        {
            var callId = await _recordings.ProcessAsync(fileName, cancellationToken);
            return Accepted(new { callId });
        }

        [HttpDelete("{fileName}")]
        public async Task<IActionResult> Delete(string fileName, CancellationToken cancellationToken)
        {
            await _recordings.DeleteAsync(fileName, cancellationToken);
            return NoContent();
        }
    }
}
