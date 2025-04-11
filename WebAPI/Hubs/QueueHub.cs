using Microsoft.AspNetCore.SignalR;

namespace HospitalQueueSystem.WebAPI.Hubs
{
    public class QueueHub : Hub
    {
        public async Task SendDoctorQueueUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveDoctorQueueUpdate", message);
        }

        public async Task SendPatientRegistered(string message)
        {
            await Clients.All.SendAsync("ReceivePatientRegistered", message);
        }
    }
}
