using HospitalQueueSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Infrastructure.Data.Configurations
{
    public class QueueEntryConfiguration : IEntityTypeConfiguration<QueueEntry>
    {
        public void Configure(EntityTypeBuilder<QueueEntry> builder)
        {
            builder.ToTable("QueueEntries");

            builder.HasKey(q => q.Id);
            builder.Property(q => q.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(q => q.PatientId).IsRequired();
            builder.Property(q => q.DoctorId).IsRequired();
            builder.Property(q => q.Status).IsRequired();
            builder.Property(q => q.QueueNumber).IsRequired();
            builder.Property(q => q.CreatedAt).IsRequired();

            builder.Property<byte[]>("RowVersion") // Shadow property for concurrency
                .IsRowVersion();
        }
    }
}
