using HospitalQueueSystem.Application.CommandModel;
using HospitalQueueSystem.Application.DTO;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HospitalQueueSystem.Application.CommandHandlers
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, ResponseResultDto<object>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        public async Task<ResponseResultDto<object>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                    return new ResponseResultDto<object> { Succeeded = false, Message = "Invalid credentials." };

                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                    return new ResponseResultDto<object> { Succeeded = false, Message = "Invalid credentials." };

                var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (await _userManager.GetTwoFactorEnabledAsync(user))
                    {
                        var twoFactorCode = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                        // TODO: Send code via email/SMS

                        return new ResponseResultDto<object>
                        {
                            Succeeded = true,
                            TwoFactorRequired = true,
                            Message = "2FA code sent to your email/phone"
                        };
                    }

                    //var token = await _tokenService.GenerateToken(user);

                    //return new ResponseResultDto<object>
                    //{
                    //    Succeeded = true,
                    //    Message = "Login successful",
                    //    Data = new { Token = token }
                    //};
                    var tokenDto = await _tokenService.GenerateToken(user);
                    return new ResponseResultDto<object>
                    {
                        Succeeded = true,
                        Message = "Login successful",
                        Data = tokenDto // <-- Not anonymous
                    };
                }

                if (result.IsLockedOut)
                    return new ResponseResultDto<object> { Succeeded = false, IsLockedOut = true, Message = "Account is locked out." };

                if (result.RequiresTwoFactor)
                    return new ResponseResultDto<object>
                    {
                        Succeeded = true,
                        TwoFactorRequired = true,
                        Message = "2FA required."
                    };

                return new ResponseResultDto<object> { Succeeded = false, Message = "Invalid login attempt." };
            }
            catch (Exception)
            {
                return new ResponseResultDto<object> { Succeeded = false, Message = "An unexpected error occurred." };
            }
        }
    }
}
