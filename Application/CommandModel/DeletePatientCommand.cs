using MediatR;

namespace HospitalQueueSystem.Application.CommandModel
{
    public class DeletePatientCommand : IRequest<bool>
    {
        public string PatientId { get; init; }

        public DeletePatientCommand(string patientId) => PatientId = patientId;
    }
}
