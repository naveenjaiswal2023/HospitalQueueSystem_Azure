using HospitalQueueSystem.Domain.Events;
using MediatR;

namespace HospitalQueueSystem.Application.Commands
{
    public class RegisterPatientCommand : IRequest<bool>
    {
        public PatientRegisteredEvent Event { get; }

        public RegisterPatientCommand(PatientRegisteredEvent @event)
        {
            Event = @event;
        }
    }
}
