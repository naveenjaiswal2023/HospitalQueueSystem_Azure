using HospitalQueueSystem.Domain.Events;
using MediatR;

namespace HospitalQueueSystem.Application.Commands
{
    public class RegisterPatientCommand : IRequest<bool>
    {
        public string Name { get; }
        public int Age { get; }
        public string Gender { get; }
        public string Department { get; }

        public RegisterPatientCommand(string name, int age, string gender, string department)
        {
            Name = name;
            Age = age;
            Gender = gender;
            Department = department;
        }
    }
}
