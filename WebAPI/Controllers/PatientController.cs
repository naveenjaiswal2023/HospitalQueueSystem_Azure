using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.Commands;
using HospitalQueueSystem.Application.Queries;
using HospitalQueueSystem.Application.Services;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        //private readonly ServiceBusSender _sender;
        private readonly ILogger<PatientController> _logger;
        //private readonly PatientService _patientService;
        private readonly IMediator _mediator;

        public PatientController(IMediator mediator, ILogger<PatientController> logger)
        {
            // _sender = client.CreateSender("patient-topic");
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator)); // Ensure mediator is injected
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Ensure logger is injected
        }

        //[HttpPost("register")]
        //public async Task<IActionResult> RegisterPatient([FromBody] PatientRegisteredEvent @event)
        //{
        //    try
        //    {
        //        // Call the service to handle both DB and message publishing
        //        await _patientService.RegisterPatientAsync(@event);

        //        return Ok(new { Message = "Patient Registered Successfully!" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error occurred while registering patient.");
        //        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while registering the patient.");
        //    }
        //}

        [HttpPost("RegisterPatient")]
        public async Task<IActionResult> RegisterPatient([FromBody] PatientRegisteredEvent @event)
        {
            try
            {
                if (@event == null)
                {
                    return BadRequest("Patient data must be provided.");
                }

                // Optional: Validate required fields
                if (string.IsNullOrWhiteSpace(@event.Name) || @event.RegisteredAt == default)
                {
                    return BadRequest("Invalid patient details.");
                }

                var command = new RegisterPatientCommand(@event);
                var result = await _mediator.Send(command);

                if (result)
                    return Ok("Patient registered successfully.");
                else
                    return StatusCode(500, "Registration failed due to an internal error.");
            }
            catch (Exception ex)
            {
                // You can inject and use ILogger<PatientController> if not already
                // _logger.LogError(ex, "Error occurred while registering patient");

                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }



        [HttpGet("GetAllPatients")]
        public async Task<IActionResult> GetAllPatients()
        {
            try
            {
                var result = await _mediator.Send(new GetAllPatientsQuery());
                return Ok(result);
            }
            catch (Exception ex)
            {
                // You could also use a logger here if available
                // _logger.LogError(ex, "Error occurred while fetching patients.");

                return StatusCode(500, $"An error occurred while retrieving patients: {ex.Message}");
            }
        }

    }
}
