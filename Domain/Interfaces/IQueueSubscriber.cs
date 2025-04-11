namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IQueueSubscriber
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}
