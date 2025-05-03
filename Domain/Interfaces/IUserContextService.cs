namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IUserContextService
    {
        string? GetUserId();
        string? GetUserName();
        string? GetUserEmail();
        List<string> GetUserRoles();
        bool IsAuthenticated();
    }
}
