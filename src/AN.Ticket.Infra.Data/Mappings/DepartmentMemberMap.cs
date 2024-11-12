using AN.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AN.Ticket.Infra.Data.Mappings;
public class DepartmentMemberMap : IEntityTypeConfiguration<DepartmentMember>
{
    public void Configure(EntityTypeBuilder<DepartmentMember> builder)
    {
        builder.ToTable("DepartmentMembers");

        builder.HasKey(dm => dm.Id);

        builder.Property(dm => dm.DepartmentId)
            .IsRequired();

        builder.HasOne(dm => dm.Department)
            .WithMany(d => d.Members)
            .HasForeignKey(dm => dm.DepartmentId);

        builder.Property(dm => dm.UserId)
            .IsRequired(false);

        builder.HasOne(dm => dm.User)
            .WithMany(u => u.Departments)
            .HasForeignKey(dm => dm.UserId);

        builder.Property(dm => dm.ContactId)
            .IsRequired(false);

        builder.HasOne(dm => dm.Contact)
            .WithMany(c => c.Departments)
            .HasForeignKey(dm => dm.ContactId);
    }
}