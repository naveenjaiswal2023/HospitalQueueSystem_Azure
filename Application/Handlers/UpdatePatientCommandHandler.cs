using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Application.Handlers
{
    public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, bool>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdatePatientCommandHandler> _logger;

        public UpdatePatientCommandHandler(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, ILogger<UpdatePatientCommandHandler> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var patient = await _dbContext.Patients.FindAsync(new object[] { request.Event.PatientId }, cancellationToken);
                    if (patient == null) return false;

                    patient.Name = request.Event.Name;
                    patient.Age = request.Event.Age;
                    patient.Gender = request.Event.Gender;
                    patient.Department = request.Event.Department;

                    _dbContext.Patients.Update(patient);
                    await _unitOfWork.SaveChangesAsync();

                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error updating patient.");
                    return false;
                }
            });
        }
    }
}
