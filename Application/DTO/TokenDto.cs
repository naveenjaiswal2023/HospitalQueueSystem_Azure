﻿namespace HospitalQueueSystem.Application.DTO
{
    public class TokenDto
    {
        public string Token { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
