using HospitalQueueSystem.Application.QuerieModel;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PodController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PodController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("queue/{doctorId}")]
        public async Task<IActionResult> GetQueue(int doctorId)
        {
            var result = await _mediator.Send(new GetQueueForPODQuery { DoctorId = doctorId });
            return Ok(result);
        }
    }

}
