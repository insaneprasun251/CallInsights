using Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace UI.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsightsController : ControllerBase
    {
        private readonly IInsightsService _insights;
        public InsightsController(IInsightsService insights)
        {
            _insights = insights;
        }

        [HttpGet("{callId:guid}")]
        public async Task<IActionResult> GetByCallId(Guid callId, CancellationToken cancellationToken)
        {
            var insights = await _insights.GetByCallIdAsync(callId, cancellationToken);
            return Ok(insights);
        }
    }
}
