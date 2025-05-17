using HospitalQueueSystem.Domain.Common;
using MediatR;

namespace HospitalQueueSystem.Domain.Events
{
    public class DoctorQueueCreatedEvent : IDomainEvent, INotification
    {
        public string PatientId { get; }
        public string DoctorId { get; }
        public string Status { get; }
        public string QueueNumber { get; }

        public DoctorQueueCreatedEvent(string patientId, string doctorId, string status, string queueNumber)
        {
            PatientId = patientId;
            DoctorId = doctorId;
            Status = status;
            QueueNumber = queueNumber;
        }

    }
}
