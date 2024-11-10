using AN.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AN.Ticket.Infra.Data.Mappings;

public class AssetAssignmentMap : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("AssetAssignments");

        builder.HasKey(aa => aa.Id);

        builder.Property(aa => aa.AssetId)
            .IsRequired();

        builder.Property(aa => aa.UserId)
            .IsRequired(false);

        builder.Property(aa => aa.ContactId)
            .IsRequired(false);

        builder.Property(aa => aa.AssignedAt)
            .IsRequired();

        builder.Property(aa => aa.ReturnedAt)
            .IsRequired(false);

        builder.HasOne(aa => aa.Asset)
            .WithMany(a => a.AssetAssignments)
            .HasForeignKey(aa => aa.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(aa => aa.User)
            .WithMany(u => u.AssetAssignments)
            .HasForeignKey(aa => aa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(aa => aa.Contact)
            .WithMany(c => c.AssetAssignments)
            .HasForeignKey(aa => aa.ContactId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}