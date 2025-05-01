using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IPublisher
    {
        Task PublishAsync<T>(T @event, string subject);
    }
}
