using HospitalQueueSystem.Application.Queries;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HospitalQueueSystem.Application.QueryHandlers
{
    public class GetAllPatientsQueryHandler : IRequestHandler<GetAllPatientsQuery, List<PatientRegisteredEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetAllPatientsQueryHandler> _logger;
        private const string PatientListCacheKey = "PatientList";

        public GetAllPatientsQueryHandler(IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<GetAllPatientsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<PatientRegisteredEvent>> Handle(GetAllPatientsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(PatientListCacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Returning patient list from Redis cache.");
                    return JsonSerializer.Deserialize<List<PatientRegisteredEvent>>(cachedData);
                }

                var patients = await _unitOfWork.Context.Patients
                    .AsNoTracking()
                    .OrderByDescending(p => p.RegisteredAt)
                    .Select(p => new PatientRegisteredEvent(
                        p.PatientId,
                        p.Name,
                        p.Age,
                        p.Gender,
                        p.Department,
                        p.RegisteredAt
                    ))
                    .ToListAsync(cancellationToken);

                if (!patients.Any())
                {
                    _logger.LogWarning("No patients found in the database.");
                    return new List<PatientRegisteredEvent>();
                }

                var serializedPatients = JsonSerializer.Serialize(patients);
                await _cache.SetStringAsync(
                    PatientListCacheKey,
                    serializedPatients,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                    });

                _logger.LogInformation("Patient list cached to Redis successfully.");
                return patients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving patient list.");
                return new List<PatientRegisteredEvent>();
            }
        }
    }
}
