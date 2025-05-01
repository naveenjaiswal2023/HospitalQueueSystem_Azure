using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IPatientCacheService
    {
        Task AddPatientToCacheAsync(PatientRegisteredEvent patient);
        Task<List<PatientRegisteredEvent>> GetQueueAsync();
    }
}
