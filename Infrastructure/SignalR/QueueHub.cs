using Microsoft.AspNetCore.SignalR;

namespace HospitalQueueSystem.Infrastructure.SignalR
{
    public class QueueHub : Hub
    {
        public async Task SendPatientCalled(int patientId, int doctorId)
        {
            await Clients.All.SendAsync("PatientCalled", patientId, doctorId);
        }
    }

}
