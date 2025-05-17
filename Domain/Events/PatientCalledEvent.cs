using HospitalQueueSystem.Domain.Common;
using MediatR;

namespace HospitalQueueSystem.Domain.Events
{
    public class PatientCalledEvent : IDomainEvent, INotification
    {
        public int PatientId { get; }
        public int DoctorId { get; }

        public PatientCalledEvent(int patientId, int doctorId)
        {
            PatientId = patientId;
            DoctorId = doctorId;
        }
    }
}
