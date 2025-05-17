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

        public IPatientRepository _PatientsRepository;
        private IQueueRepository _queueRepository;

        public ApplicationDbContext Context => _dbContext;

       // public IPatientRepository Patients => throw new NotImplementedException();
        public IPatientRepository PatientRepository
        {
            get
            {
                return _PatientsRepository ??= new PatientRepository(_dbContext);
            }
        }

        public IQueueRepository QueueRepository
        {
            get
            {
                return _queueRepository ??= new QueueRepository(_dbContext);
            }
        }

        public UnitOfWork(ApplicationDbContext dbContext, IPatientRepository patientRepository, IQueueRepository queueRepository)
        {
            _dbContext = dbContext;
            _PatientsRepository = patientRepository;
            _queueRepository = queueRepository;
        }

        // Start a new transaction
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
            }

            _transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel);
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch (Exception)
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while saving changes.", ex);
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _dbContext.Dispose();
        }
    }

}
