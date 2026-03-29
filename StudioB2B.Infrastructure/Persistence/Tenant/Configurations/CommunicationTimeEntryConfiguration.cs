using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class CommunicationTimeEntryConfiguration : IEntityTypeConfiguration<CommunicationTimeEntry>
{
    public void Configure(EntityTypeBuilder<CommunicationTimeEntry> builder)
    {
        builder.ToTable("CommunicationTimeEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.Ignore(x => x.Duration);

        builder.HasOne(x => x.Task)
            .WithMany(t => t.TimeEntries)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.StartedAt);
    }
}
