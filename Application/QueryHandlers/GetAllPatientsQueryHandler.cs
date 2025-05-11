using HospitalQueueSystem.Application.Queries;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MediatR;

namespace HospitalQueueSystem.Application.QueryHandlers
{
    public class GetAllPatientsQueryHandler : IRequestHandler<GetAllPatientsQuery, List<PatientRegisteredEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GetAllPatientsQueryHandler> _logger;
        private const string PatientListCacheKey = "PatientList";

        public GetAllPatientsQueryHandler(IUnitOfWork unitOfWork, IMemoryCache cache, ILogger<GetAllPatientsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<PatientRegisteredEvent>> Handle(GetAllPatientsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (_cache.TryGetValue(PatientListCacheKey, out List<PatientRegisteredEvent> cachedPatients))
                {
                    _logger.LogInformation("Returning patient list from cache.");
                    return cachedPatients;
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

                _cache.Set(PatientListCacheKey, patients, TimeSpan.FromMinutes(5));
                _logger.LogInformation("Patient list cached successfully.");
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
