using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Application.Commands;
using HospitalQueueSystem.Application.DTO;
using HospitalQueueSystem.Application.Queries;
using HospitalQueueSystem.Application.Services;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly ILogger<PatientController> _logger;
        private readonly IMediator _mediator;

        public PatientController(IMediator mediator, ILogger<PatientController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator)); // Ensure mediator is injected
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Ensure logger is injected
        }

        [HttpPost("RegisterPatient")]
        public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDto dto)
        {
            try
            {
                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.Name) ||
                    string.IsNullOrWhiteSpace(dto.Gender) ||
                    string.IsNullOrWhiteSpace(dto.Department) ||
                    dto.Age <= 0)
                {
                    return BadRequest("Invalid patient data.");
                }

                var command = new RegisterPatientCommand(dto.Name, dto.Age, dto.Gender, dto.Department);
                var result = await _mediator.Send(command);

                if (result)
                    return Ok("Patient registered successfully.");
                else
                    return StatusCode(500, "Registration failed due to an internal error.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering patient");
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
                _logger.LogError(ex, "Error occurred while fetching patients.");

                return StatusCode(500, $"An error occurred while retrieving patients: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(string id, [FromBody] PatientRegisteredEvent model)
        {
            try
            {
                if (id != model.PatientId)
                    return BadRequest("Patient ID mismatch.");

                var command = new UpdatePatientCommand(model);
                var result = await _mediator.Send(command);

                if (!result)
                    return NotFound("Patient not found or update failed.");

                return Ok("Patient updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID {PatientId}", id);
                return StatusCode(500, "An error occurred while updating the patient.");
            }
        }

        // DELETE: api/patient/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(string id)
        {
            try
            {
                var command = new DeletePatientCommand(id);
                var result = await _mediator.Send(command);

                if (!result)
                    return NotFound("Patient not found or deletion failed.");

                return Ok("Patient deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID {PatientId}", id);
                return StatusCode(500, "An error occurred while deleting the patient.");
            }
        }
    }
}
