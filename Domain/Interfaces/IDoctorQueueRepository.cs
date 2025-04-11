using HospitalQueueSystem.Domain.Entities;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IDoctorQueueRepository
    {
        Task AddAsync(DoctorQueue doctorQueue);
        Task<DoctorQueue?> GetByDoctorIdAsync(int doctorId);
        Task<List<DoctorQueue>> GetAllAsync();
        Task UpdateAsync(DoctorQueue doctorQueue);
        Task DeleteAsync(int id);
    }
}
