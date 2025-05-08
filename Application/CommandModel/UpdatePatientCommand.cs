using HospitalQueueSystem.Domain.Events;
using MediatR;

namespace HospitalQueueSystem.Application.CommandModel
{
    public class UpdatePatientCommand : IRequest<bool>
    {
        public PatientRegisteredEvent Event { get; set; }

        public UpdatePatientCommand(PatientRegisteredEvent @event)
        {
            Event = @event;
        }
    }
}
