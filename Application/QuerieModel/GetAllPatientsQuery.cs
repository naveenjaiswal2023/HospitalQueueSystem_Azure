using HospitalQueueSystem.Domain.Events;
using MediatR;

namespace HospitalQueueSystem.Application.Queries
{
    public class GetAllPatientsQuery : IRequest<List<PatientRegisteredEvent>>
    {

    }
}
