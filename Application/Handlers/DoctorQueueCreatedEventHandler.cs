using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using HospitalQueueSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HospitalQueueSystem.Application.Handlers
{
    public class DoctorQueueCreatedEventHandler
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<QueueHub> _hubContext;

        public DoctorQueueCreatedEventHandler(IUnitOfWork unitOfWork, IHubContext<QueueHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task HandleAsync(DoctorQueueCreatedEvent @event)
        {
            var doctorQueue = new DoctorQueue
            {
                DoctorId = @event.DoctorId,
                QueueNumber = @event.QueueNumber,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DoctorQueues.AddAsync(doctorQueue);
            await _unitOfWork.CompleteAsync();

            await _hubContext.Clients.All.SendAsync("DoctorQueueUpdated", doctorQueue);
        }
    }

}
