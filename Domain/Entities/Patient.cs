using HospitalQueueSystem.Domain.Common;
using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Domain.Entities
{
    public class Patient : BaseEntity
    {
        public string PatientId { get; private set; }
        public string Name { get; private set; }
        public int Age { get; private set; }
        public string Gender { get; private set; }
        public string Department { get; private set; }
        public DateTime RegisteredAt { get; private set; }

        public Patient(string name, int age, string gender, string department)
        {
            PatientId = Guid.NewGuid().ToString();
            Name = name;
            Age = age;
            Gender = gender;
            Department = department;
            RegisteredAt = DateTime.UtcNow;

            // Raise the domain event
            AddDomainEvent(new PatientRegisteredEvent(PatientId, Name, Age, Gender, Department, RegisteredAt));
        }
        public void UpdateDetails(string name, int age, string gender, string department)
        {
            Name = name;
            Age = age;
            Gender = gender;
            Department = department;

            AddDomainEvent(new PatientUpdatedEvent(PatientId, name, age, gender, department));
        }

        public void MarkAsDeleted()
        {
            AddDomainEvent(new PatientDeletedEvent(PatientId));
        }
    }
}