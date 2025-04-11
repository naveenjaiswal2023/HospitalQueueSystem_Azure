using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Repositories;

namespace HospitalQueueSystem.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            DoctorQueues = new DoctorQueueRepository(_context);
            Patients = new PatientRepository(_context);
        }

        public IDoctorQueueRepository DoctorQueues { get; private set; }
        public IPatientRepository Patients { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
