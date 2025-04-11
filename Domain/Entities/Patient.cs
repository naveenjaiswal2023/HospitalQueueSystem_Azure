namespace HospitalQueueSystem.Domain.Entities
{
    public class Patient
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
