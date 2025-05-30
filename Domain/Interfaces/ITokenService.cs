using HospitalQueueSystem.Application.DTO;
using HospitalQueueSystem.Infrastructure.Data;

namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface ITokenService
    {
        Task<TokenDto> GenerateToken(ApplicationUser user);
    }
}
