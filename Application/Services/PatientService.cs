//using HospitalQueueSystem.Domain.Entities;
//using HospitalQueueSystem.Domain.Events;
//using HospitalQueueSystem.Domain.Interfaces;
//using HospitalQueueSystem.Infrastructure.AzureBus;
//using HospitalQueueSystem.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Logging;

//namespace HospitalQueueSystem.Application.Services
//{
//    public class PatientService
//    {
//        private readonly ApplicationDbContext _dbContext;
//        private readonly IPublisher _publisher;
//        private readonly ILogger<PatientService> _logger;
//        private readonly IMemoryCache _cache;
//        private const string PatientListCacheKey = "PatientList";
//        private readonly IUnitOfWork _unitOfWork;
//        public PatientService(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, IPublisher publisher, ILogger<PatientService> logger, IMemoryCache cache)
//        {
//            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
//            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            _cache = cache ?? throw new ArgumentNullException(nameof(cache)); // FIXED
//            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
//        }

//        public async Task RegisterPatientAsync(PatientRegisteredEvent @event)
//        {
//            try
//            {
//                await _unitOfWork.BeginTransactionAsync();

//                var patient = new Patient
//                {
//                    PatientId = @event.PatientId,
//                    Name = @event.Name,
//                    Age = @event.Age,
//                    Gender = @event.Gender,
//                    Department = @event.Department,
//                    RegisteredAt = @event.RegisteredAt
//                };

//                _dbContext.Patients.Add(patient);
//                await _unitOfWork.SaveChangesAsync();

//                await _publisher.PublishAsync(@event, nameof(PatientRegisteredEvent));

//                await _unitOfWork.CommitTransactionAsync();
//            }
//            catch (Exception ex)
//            {
//                await _unitOfWork.RollbackTransactionAsync();
//                _logger.LogError(ex, "Error registering patient.");
//                throw;
//            }
//        }

//        public async Task<List<PatientRegisteredEvent>> GetAllPatientsAsync()
//        {
//            try
//            {
//                // Try to get from cache
//                if (_cache.TryGetValue(PatientListCacheKey, out List<PatientRegisteredEvent> cachedPatients))
//                {
//                    _logger.LogInformation("Returning patient list from cache.");
//                    return cachedPatients;
//                }

//                // Access context via UnitOfWork
//                var patients = await _unitOfWork.Context.Patients
//                    .AsNoTracking()
//                    .OrderByDescending(p => p.RegisteredAt)
//                    .Select(p => new PatientRegisteredEvent
//                    {
//                        PatientId = p.PatientId,
//                        Name = p.Name,
//                        Age = p.Age,
//                        Gender = p.Gender,
//                        Department = p.Department,
//                        RegisteredAt = p.RegisteredAt
//                    })
//                    .ToListAsync();

//                if (patients == null || !patients.Any())
//                {
//                    _logger.LogWarning("No patients found in the database.");
//                    return new List<PatientRegisteredEvent>();
//                }

//                _cache.Set(PatientListCacheKey, patients, TimeSpan.FromMinutes(5));
//                _logger.LogInformation("Patient list cached successfully.");

//                return patients;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while retrieving patient list.");
//                return new List<PatientRegisteredEvent>(); // fallback
//            }
//        }
//    }
//}
