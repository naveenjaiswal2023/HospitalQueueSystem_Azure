using HospitalQueueSystem.Domain.Entities;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IQueueRepository
    {
        Task<QueueEntry> GetByIdAsync(string id);
        Task<List<QueueEntry>> GetQueueByDoctorIdAsync(int doctorId);
        Task AddAsync(QueueEntry entry);
        Task UpdateAsync(QueueEntry entry);
    }

}
