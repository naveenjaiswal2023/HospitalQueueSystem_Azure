using HospitalQueueSystem.Infrastructure.Data;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser user);
    }
}
