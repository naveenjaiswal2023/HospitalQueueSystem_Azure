using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace HospitalQueueSystem.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
        }

        [HttpPost("register-user")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User"); // default role
            return Ok("User created successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid login request.");
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Unauthorized("Invalid credentials.");
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordValid)
                {
                    return Unauthorized("Invalid credentials.");
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Token generation here (after login success and no 2FA)
                    if (await _userManager.GetTwoFactorEnabledAsync(user))
                    {
                        var twoFactorCode = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

                        // TODO: Send `twoFactorCode` via email/SMS
                        return Ok(new
                        {
                            twoFactorRequired = true,
                            message = "2FA code sent to your email/phone"
                        });
                    }

                    // 🔑 Generate JWT token
                    var token = _tokenService.GenerateToken(user);

                    return Ok(new
                    {
                        message = "Login successful",
                        token
                    });
                }

                if (result.IsLockedOut)
                {
                    return Forbid("Account is locked out.");
                }

                if (result.RequiresTwoFactor)
                {
                    return Ok(new
                    {
                        twoFactorRequired = true,
                        message = "2FA required."
                    });
                }

                return Unauthorized("Invalid login attempt.");
            }
            catch (Exception ex)
            {
                // Log error here
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        [HttpPost("verify-twofactor")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerificationModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.TwoFactorCode,model.RememberMe,model.RememberMachine);

            if (result.Succeeded)
            {
                return Ok(new { message = "Login successful" });
            }

            return Unauthorized("Invalid 2FA code.");
        }

    }

}
