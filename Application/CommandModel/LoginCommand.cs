using HospitalQueueSystem.Application.DTO;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HospitalQueueSystem.Application.CommandModel
{
    public class LoginCommand : IRequest<ResponseResultDto<object>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
