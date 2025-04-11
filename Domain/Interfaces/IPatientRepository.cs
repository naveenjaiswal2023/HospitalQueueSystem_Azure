using HospitalQueueSystem.Domain.Entities;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IPatientRepository
    {
        Task AddAsync(Patient patient);
        Task<Patient?> GetByIdAsync(int id);
        Task<List<Patient>> GetAllAsync();
        Task UpdateAsync(Patient patient);
        Task DeleteAsync(int id);
    }
}
