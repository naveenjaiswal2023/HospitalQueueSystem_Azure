namespace HospitalQueueSystem.Shared.Helpers
{
    public class IdGenerator
    {
        public static string GenerateId(string prefix)
        {
            return $"{prefix}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
    }
}
