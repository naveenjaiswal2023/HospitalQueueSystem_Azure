namespace HospitalQueueSystem.Domain.Entities
{
    public class Patient
    {
        public string PatientId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }

        public string Department { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
