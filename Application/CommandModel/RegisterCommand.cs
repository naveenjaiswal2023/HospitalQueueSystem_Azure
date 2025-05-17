using HospitalQueueSystem.Domain.Interfaces;
using MediatR;

namespace HospitalQueueSystem.Application.CommandModel
{
    public class RegisterCommand : IRequest<ICommandResult>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
