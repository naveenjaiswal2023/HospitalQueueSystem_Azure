using HospitalQueueSystem.Infrastructure.Data;
using System.Data;
using System.Threading.Tasks;
namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        ApplicationDbContext Context { get; }
        Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> SaveChangesAsync();
    }
}
