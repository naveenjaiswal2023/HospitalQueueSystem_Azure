using HospitalQueueSystem.Domain.Common;
using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Application.Common
{
    public interface IDomainEventPublisher
    {
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
    }
}
