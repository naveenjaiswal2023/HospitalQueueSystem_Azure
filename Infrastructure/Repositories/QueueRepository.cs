using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Infrastructure.Repositories
{
    public class QueueRepository : IQueueRepository
    {
        private readonly ApplicationDbContext _context;

        public QueueRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QueueEntry> GetByIdAsync(string id)
        {
            return await _context.QueueEntries.FindAsync(id);
        }

        public async Task<List<QueueEntry>> GetQueueByDoctorIdAsync(int doctorId)
        {
            return await _context.QueueEntries
                .Where(q => q.DoctorId == doctorId.ToString() && q.Status == "Called")
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();
        }

        public Task AddAsync(QueueEntry entry)
        {
            _context.QueueEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(QueueEntry entry)
        {
            _context.QueueEntries.Update(entry);
            return Task.CompletedTask;
        }
    }
}
