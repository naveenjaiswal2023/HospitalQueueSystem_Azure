using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace HospitalQueueSystem.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IDbContextTransaction _transaction;

        public ApplicationDbContext Context => _dbContext;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            => _transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel);
        public async Task CommitTransactionAsync() => await _transaction?.CommitAsync();
        public async Task RollbackTransactionAsync() => await _transaction?.RollbackAsync();
    }
}
