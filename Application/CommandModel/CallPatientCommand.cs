using MediatR;

namespace HospitalQueueSystem.Application.CommandModel
{
    public class CallPatientCommand : IRequest<bool>
    {
        public int QueueEntryId { get; set; }
    }
}
