using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Application.Commands;
using HospitalQueueSystem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Application.Common;

namespace HospitalQueueSystem.Application.Handlers
{
    public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeletePatientCommandHandler> _logger;
        private readonly IDomainEventPublisher _domainEventPublisher;

        public DeletePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<DeletePatientCommandHandler> logger,
                                            IDomainEventPublisher domainEventPublisher)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _domainEventPublisher = domainEventPublisher;
        }

        public async Task<bool> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _unitOfWork.Context.Patients
                    .FirstOrDefaultAsync(p => p.PatientId == request.PatientId, cancellationToken);

                if (patient == null)
                {
                    _logger.LogWarning("Patient with ID {PatientId} not found.", request.PatientId);
                    return false;
                }

                // Mark the patient as deleted by raising a domain event
                patient.MarkAsDeleted();

                // Remove the patient from the database
                _unitOfWork.Context.Patients.Remove(patient);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Now publish the domain event
                foreach (var domainEvent in patient.DomainEvents)
                {
                    await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
                }

                // Clear domain events after publishing
                patient.ClearDomainEvents();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID {PatientId}", request.PatientId);
                return false;
            }
        }
    }
}