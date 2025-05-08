using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using HospitalQueueSystem.Application.CommandModel;

namespace HospitalQueueSystem.Application.Handlers
{
    public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<DeletePatientCommandHandler> _logger;

        public DeletePatientCommandHandler(IUnitOfWork unitOfWork, IPatientRepository patientRepository, ILogger<DeletePatientCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _patientRepository = patientRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _patientRepository.DeleteAsync(request.PatientId);
                if (result <= 0)
                {
                    _logger.LogWarning("No rows affected during patient deletion.");
                    return false; // Deletion failed
                }

                return true; // Successfully deleted
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient.");
                return false; // Error during deletion
            }
        }
    }
}
