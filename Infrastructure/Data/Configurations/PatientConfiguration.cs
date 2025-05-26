using HospitalQueueSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Infrastructure.Data.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.HasKey(p => p.PatientId);
            builder.Property(p => p.Name).IsRequired();
            builder.Property(p => p.Department).IsRequired();
            builder.Property(p => p.RegisteredAt).IsRequired();
        }
    }
}
