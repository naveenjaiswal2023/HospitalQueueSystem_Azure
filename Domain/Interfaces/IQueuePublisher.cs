using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IQueuePublisher
    {
        Task PublishDoctorQueueAsync(DoctorQueueCreatedEvent @event);
        Task PublishPatientRegisteredAsync(PatientRegisteredEvent @event);
    }
}
