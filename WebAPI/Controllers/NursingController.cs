using HospitalQueueSystem.Application.CommandModel;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NursingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NursingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("call-patient")]
        public async Task<IActionResult> CallPatient([FromBody] CallPatientCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok() : BadRequest("Patient not found");
        }
    }

}
