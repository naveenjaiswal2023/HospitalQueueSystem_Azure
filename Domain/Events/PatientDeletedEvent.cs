using HospitalQueueSystem.Domain.Common;
using MediatR;

namespace HospitalQueueSystem.Domain.Events
{
    public class PatientDeletedEvent : IDomainEvent, INotification
    {
        public string PatientId { get; }

        public PatientDeletedEvent(string patientId)
        {
            PatientId = patientId;
        }
    }
}
