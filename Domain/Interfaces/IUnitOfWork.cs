using HospitalQueueSystem.Infrastructure.Data;
using System.Data;
using System.Threading.Tasks;
namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ApplicationDbContext Context { get; }
        IPatientRepository PatientRepository { get; }
        IQueueRepository QueueRepository { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }

}
