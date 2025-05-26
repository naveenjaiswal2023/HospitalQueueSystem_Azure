using HospitalQueueSystem.Domain.Events;
using MediatR;

namespace HospitalQueueSystem.Application.CommandModel
{
    public class UpdatePatientCommand : IRequest<bool>
    {
        public string PatientId { get; }
        public string Name { get; }
        public int Age { get; }
        public string Gender { get; }
        public string Department { get; }

        public UpdatePatientCommand(string patientId, string name, int age, string gender, string department)
        {
            PatientId = patientId;
            Name = name;
            Age = age;
            Gender = gender;
            Department = department;
        }

        // Optional constructor overload for convenience if you're passing an event/model
        public UpdatePatientCommand(PatientUpdatedEvent patient)
        {
            PatientId = patient.PatientId;
            Name = patient.Name;
            Age = patient.Age;
            Gender = patient.Gender;
            Department = patient.Department;
        }
    }
}
