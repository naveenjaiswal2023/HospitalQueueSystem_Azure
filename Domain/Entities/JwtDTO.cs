﻿namespace HospitalQueueSystem.Domain.Entities
{
    public class JwtDTO
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiryMinutes { get; set; }
    }
}
