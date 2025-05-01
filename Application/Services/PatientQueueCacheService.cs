using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HospitalQueueSystem.Application.Services
{
    public class PatientQueueCacheService : IPatientCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private const string CacheKey = "PatientQueue";

        public PatientQueueCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task AddPatientToCacheAsync(PatientRegisteredEvent patient)
        {
            var patients = _memoryCache.Get<List<PatientRegisteredEvent>>(CacheKey) ?? new List<PatientRegisteredEvent>();
            patients.Add(patient);
            _memoryCache.Set(CacheKey, patients, TimeSpan.FromMinutes(5)); 
            return Task.CompletedTask;
        }

        public Task<List<PatientRegisteredEvent>> GetQueueAsync()
        {
            var patients = _memoryCache.Get<List<PatientRegisteredEvent>>(CacheKey) ?? new List<PatientRegisteredEvent>();
            return Task.FromResult(patients);
        }
    }
}
