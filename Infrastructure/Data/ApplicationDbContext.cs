using HospitalQueueSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HospitalQueueSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<DoctorQueue> DoctorQueues { get; set; }
        public DbSet<Patient> Patients { get; set; }
    }
}
