using AN.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AN.Ticket.Infra.Data.Mappings;

public class AssetMap : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.SerialNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.AssetType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.PurchaseDate)
            .IsRequired();

        builder.Property(a => a.Value)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.HasMany(a => a.AssetAssignments)
            .WithOne(aa => aa.Asset)
            .HasForeignKey(aa => aa.AssetId);

        builder.HasMany(a => a.AssetFiles)
            .WithOne(af => af.Asset)
            .HasForeignKey(af => af.AssetId);
    }
}