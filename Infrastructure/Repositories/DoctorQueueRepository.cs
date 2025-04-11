using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Infrastructure.Repositories
{
    public class DoctorQueueRepository : IDoctorQueueRepository
    {
        private readonly ApplicationDbContext _context;

        public DoctorQueueRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DoctorQueue doctorQueue)
        {
            await _context.DoctorQueues.AddAsync(doctorQueue);
            await _context.SaveChangesAsync();
        }

        public async Task<DoctorQueue?> GetByDoctorIdAsync(int doctorId)
        {
            return await _context.DoctorQueues
                .FirstOrDefaultAsync(q => q.DoctorId == doctorId.ToString());
        }

        public async Task<List<DoctorQueue>> GetAllAsync()
        {
            return await _context.DoctorQueues.ToListAsync();
        }

        public async Task UpdateAsync(DoctorQueue doctorQueue)
        {
            _context.DoctorQueues.Update(doctorQueue);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.DoctorQueues.FindAsync(id);
            if (entity != null)
            {
                _context.DoctorQueues.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
