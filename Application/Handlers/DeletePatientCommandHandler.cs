using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Application.Handlers
{
    public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, bool>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeletePatientCommandHandler> _logger;

        public DeletePatientCommandHandler(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, ILogger<DeletePatientCommandHandler> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var patient = await _dbContext.Patients.FindAsync(new object[] { request.PatientId }, cancellationToken);
                    if (patient == null) return false;

                    _dbContext.Patients.Remove(patient);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error deleting patient.");
                    return false;
                }
            });
        }
    }
}
