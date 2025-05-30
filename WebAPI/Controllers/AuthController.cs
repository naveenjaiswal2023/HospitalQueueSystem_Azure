using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using HospitalQueueSystem.Shared.Utilities;
using Microsoft.AspNetCore.Authorization;
using HospitalQueueSystem.Application.CommandModel;
using MediatR;
using HospitalQueueSystem.Application.DTO;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserContextService _userContextService;
        private readonly IWebHostEnvironment _env;
        private readonly IMediator _mediator;
        public AuthController(IUserContextService userContextService,UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService, IWebHostEnvironment env, IMediator mediator)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _userContextService = userContextService;
            _env = env;
            _mediator=mediator;
        }

        [HttpPost("register-user")]
        [Authorize]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var command = new RegisterCommand
            {
                Email = model.Email,
                Password = model.Password
            };

            var result = await _mediator.Send(command);

            if (!result.Succeeded)
                return StatusCode(result.StatusCode, result.Response);

            return Ok(result.Response);
        }

        [HttpGet("whoami")]
        [Authorize]
        public IActionResult WhoAmI()
        {
            var userId = _userContextService.GetUserId();
            var userName = _userContextService.GetUserName();
            var roles = _userContextService.GetUserRoles();

            return Ok(new
            {
                UserId = userId,
                UserName = userName,
                Roles = roles
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    return Forbid(result.Message);

                return Unauthorized(result.Message);
            }

            if (result.TwoFactorRequired)
            {
                return Ok(new
                {
                    twoFactorRequired = true,
                    message = result.Message
                });
            }

            //var response = new ResponseResultDto<TokenDto>
            //{
            //    Succeeded = result.Succeeded,
            //    Message = result.Message,
            //    Data = new TokenDto
            //    {
            //        Token = ((dynamic?)result.Data)?.Token,
            //        //RefreshToken = ((dynamic?)result.Data)?.RefreshToken,
            //        Expiration = ((dynamic?)result.Data)?.Expiration,
            //        // UserId and Role can be added if needed
            //        //UserId = ((dynamic?)result.Data)?.UserId,
            //        //Role = ((dynamic?)result.Data)?.Role

            //    }
            //};

            var response = new ResponseResultDto<TokenDto>
            {
                Succeeded = result.Succeeded,
                Message = result.Message,
                Data = (TokenDto)result.Data // ✅ Strongly typed
            };

            return Ok(response);
        }


        [HttpPost("verify-twofactor")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerificationModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (_env.IsDevelopment()) // Only in Development!
            {
                await _signInManager.SignInAsync(user, model.RememberMe);
                return Ok(new { message = "Login successful (2FA bypassed in development)" });
            }

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                model.TwoFactorCode.Replace(" ", "").Replace("-", ""),
                model.RememberMe,
                model.RememberMachine
            );

            if (result.Succeeded)
            {
                return Ok(new { message = "Login successful" });
            }

            return Unauthorized("Invalid 2FA code.");
        }

    }

}
