using System.Threading.Tasks;
namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        IDoctorQueueRepository DoctorQueues { get; }
        IPatientRepository Patients { get; }

        Task<int> CompleteAsync();
    }
}
