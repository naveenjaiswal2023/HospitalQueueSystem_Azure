namespace HospitalQueueSystem.Domain.Events
{
    public class DoctorQueueCreatedEvent
    {
        public string DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int StartingToken { get; set; } = 1;
        public int QueueNumber { get; set; } // 👈 Ensure this line is present
        
    }
}
