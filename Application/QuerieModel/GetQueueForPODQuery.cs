using HospitalQueueSystem.Domain.Entities;
using MediatR;

namespace HospitalQueueSystem.Application.QuerieModel
{
    public class GetQueueForPODQuery : IRequest<List<QueueEntry>>
    {
        public int DoctorId { get; set; }
    }

}
