using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HospitalQueueSystem.Application.Handlers
{
    public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdatePatientCommandHandler> _logger;

        public UpdatePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdatePatientCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var rowsAffected = await _unitOfWork.Patients.UpdateAsync(request.Event);

                if (rowsAffected > 0)
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return true;
                }

                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error occurred while updating patient.");
                return false;
            }
        }
    }
}
