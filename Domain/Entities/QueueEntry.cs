using HospitalQueueSystem.Domain.Common;
using HospitalQueueSystem.Domain.Events;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HospitalQueueSystem.Domain.Entities
{
    public class QueueEntry : BaseEntity
    {
        public string Id { get; set; }
        public string PatientId { get; private set; }
        public string DoctorId { get; private set; }
        public string Status { get; private set; } // Waiting, Called, Skipped
        public string QueueNumber { get; private set; }
        //public DateTime CreatedAt { get; private set; }

        public QueueEntry(string patientId, string doctorId, string status, string queueNumber)
        {
            Id = Guid.NewGuid().ToString();
            PatientId = patientId;
            DoctorId = doctorId;
            Status = status;
            QueueNumber = queueNumber;
            CreatedAt = DateTime.UtcNow;

            // Raise the domain event
            AddDomainEvent(new DoctorQueueCreatedEvent(patientId, doctorId, status, queueNumber));
            QueueNumber = queueNumber;
        }

        public void UpdateStatus(string newStatus)
        {
            Status = newStatus;
            // Optionally, raise an event if status update is significant
        }
    }
}