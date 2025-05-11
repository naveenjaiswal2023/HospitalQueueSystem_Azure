using HospitalQueueSystem.Domain.Common;
using MediatR;

namespace HospitalQueueSystem.Domain.Events
{
    public class PatientUpdatedEvent : IDomainEvent, INotification
    {
        public string PatientId { get; }
        public string Name { get; }
        public int Age { get; }
        public string Gender { get; }
        public string Department { get; }

        public PatientUpdatedEvent(string patientId, string name, int age, string gender, string department)
        {
            PatientId = patientId;
            Name = name;
            Age = age;
            Gender = gender;
            Department = department;
        }
    }
}
