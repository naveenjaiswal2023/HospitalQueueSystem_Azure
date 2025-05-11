using HospitalQueueSystem.Domain.Events;

namespace HospitalQueueSystem.Application.Handlers
{
    public class PatientRegisteredEventHandler
    {
        public Task HandleAsync(PatientRegisteredEvent @event)
        {
            // Handle the patient registered event
            return Task.CompletedTask;
        }
    }
}
