using AN.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AN.Ticket.Infra.Data.Mappings;
public class AssetFileMap : IEntityTypeConfiguration<AssetFile>
{
    public void Configure(EntityTypeBuilder<AssetFile> builder)
    {
        builder.ToTable("AssetFiles");

        builder.HasKey(af => af.Id);

        builder.Property(af => af.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(af => af.FileContent)
            .IsRequired();

        builder.HasOne(af => af.Asset)
            .WithMany(a => a.AssetFiles)
            .HasForeignKey(af => af.AssetId);
    }
}
