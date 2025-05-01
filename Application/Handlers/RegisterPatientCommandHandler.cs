using HospitalQueueSystem.Application.Commands;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using MediatR;
using DomainPublisher = HospitalQueueSystem.Domain.Interfaces.IPublisher;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
namespace HospitalQueueSystem.Application.Handlers
{
    public class RegisterPatientCommandHandler : IRequestHandler<RegisterPatientCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly DomainPublisher _publisher;
        private readonly ILogger<RegisterPatientCommandHandler> _logger;

        public RegisterPatientCommandHandler(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, DomainPublisher publisher, ILogger<RegisterPatientCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<bool> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var patient = new Patient
                    {
                        PatientId = request.Event.PatientId,
                        Name = request.Event.Name,
                        Age = request.Event.Age,
                        Gender = request.Event.Gender,
                        Department = request.Event.Department,
                        RegisteredAt = request.Event.RegisteredAt
                    };

                    _dbContext.Patients.Add(patient);
                    await _unitOfWork.SaveChangesAsync();

                    await _publisher.PublishAsync(request.Event, nameof(PatientRegisteredEvent));

                    await transaction.CommitAsync(cancellationToken);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error registering patient.");
                    return false;
                }
            });
        }
    }
}
