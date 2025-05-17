namespace HospitalQueueSystem.Application.DTO
{
    public class ResponseResultDto<T>
    {
        public bool Succeeded { get; set; }
        public bool TwoFactorRequired { get; set; }
        public bool IsLockedOut { get; set; }
        public string Message { get; set; }
        public T? Data { get; set; }
    }
}
