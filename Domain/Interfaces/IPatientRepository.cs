using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IPatientRepository
    {
        Task AddAsync(Patient patient);
        Task<Patient?> GetByIdAsync(int id);
        Task<List<Patient>> GetAllAsync();
        Task<int> UpdateAsync(Patient model);
        Task<int> DeleteAsync(string patientId);

    }
}
