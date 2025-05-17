using HospitalQueueSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HospitalQueueSystem.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

        public DbSet<DoctorQueue> DoctorQueues { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<QueueEntry> QueueEntry { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(p => p.PatientId);
                entity.Property(p => p.Name).IsRequired();
                entity.Property(p => p.Department).IsRequired();
                entity.Property(p => p.RegisteredAt).IsRequired();
            });

            modelBuilder.Entity<QueueEntry>(entity =>
            {
                entity.ToTable("QueueEntry");

                entity.HasKey(q => q.Id);
                entity.Property(q => q.Id)
                      .IsRequired()
                      .ValueGeneratedNever(); // Because we're using a string GUID

                entity.Property(q => q.PatientId).IsRequired();
                entity.Property(q => q.DoctorId).IsRequired();
                entity.Property(q => q.Status).IsRequired();
                entity.Property(q => q.QueueNumber).IsRequired();
                entity.Property(q => q.CreatedAt).IsRequired();
            });
        }

    }
}
