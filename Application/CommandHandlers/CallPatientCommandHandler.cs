using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Application.Common;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HospitalQueueSystem.Application.CommandHandlers
{
    public class CallPatientCommandHandler : IRequestHandler<CallPatientCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<CallPatientCommandHandler> _logger;
        private readonly IDomainEventPublisher _domainEventPublisher;

        public CallPatientCommandHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<CallPatientCommandHandler> logger, 
            IDomainEventPublisher domainEventPublisher)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
            _domainEventPublisher = domainEventPublisher;
        }

        public async Task<bool> Handle(CallPatientCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Convert QueueEntryId to string before passing to GetByIdAsync
                var entry = await _unitOfWork.QueueRepository.GetByIdAsync(request.QueueEntryId.ToString());
                if (entry == null)
                {
                    _logger.LogWarning("Queue entry not found for ID: {QueueEntryId}", request.QueueEntryId);
                    return false;
                }

                // Update status
                entry.UpdateStatus("Called");

                // Persist changes
                await _unitOfWork.QueueRepository.UpdateAsync(entry);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Convert PatientId and DoctorId to integers before passing to PatientCalledEvent
                if (!int.TryParse(entry.PatientId, out var patientId))
                {
                    _logger.LogError("Invalid PatientId: {PatientId}", entry.PatientId);
                    return false;
                }

                if (!int.TryParse(entry.DoctorId, out var doctorId))
                {
                    _logger.LogError("Invalid DoctorId: {DoctorId}", entry.DoctorId);
                    return false;
                }

                // Publish domain events
                foreach (var domainEvent in entry.DomainEvents)
                {
                    await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
                }

                entry.ClearDomainEvents();
                _logger.LogInformation("Patient {PatientId} Called successfully.", entry.PatientId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling patient for QueueEntryId: {QueueEntryId}", request.QueueEntryId);
                return false;
            }
        }
    }
}
