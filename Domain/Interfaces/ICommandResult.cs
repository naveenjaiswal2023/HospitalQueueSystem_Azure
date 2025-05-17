namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface ICommandResult
    {
        bool Succeeded { get; }
        object Response { get; }
        int StatusCode { get; }
    }
}
