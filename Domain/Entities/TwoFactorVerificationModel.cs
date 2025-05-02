using System.ComponentModel.DataAnnotations;

namespace HospitalQueueSystem.Domain.Entities
{
    public class TwoFactorVerificationModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Verification Code")]
        public string TwoFactorCode { get; set; }

        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }
    }
}
