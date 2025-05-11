using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
    //    private readonly IPublisher _publisher;

    //    public QueueController(IPublisher publisher)
    //    {
    //        _publisher = publisher;
    //    }

    //    [HttpPost("generate-doctor-queue")]
    //    public async Task<IActionResult> GenerateDoctorQueue([FromBody] DoctorQueueCreatedEvent @event)
    //    {
    //        //await _publisher.PublishDoctorQueueAsync(@event);
    //        return Ok(new { message = "Doctor queue generation event published." });
    //    }
    }
}
