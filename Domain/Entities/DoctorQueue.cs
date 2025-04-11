namespace HospitalQueueSystem.Domain.Entities
{
    public class DoctorQueue
    {
        public int Id { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int CurrentToken { get; set; }
        public int QueueNumber { get; set; } // 👈 Ensure this line is present
        public string StartingToken { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
       
    }
}
