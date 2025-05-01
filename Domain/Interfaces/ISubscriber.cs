namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface ISubscriber<in TEvent>
    {
        Task HandleAsync(TEvent @event);
    }
}
