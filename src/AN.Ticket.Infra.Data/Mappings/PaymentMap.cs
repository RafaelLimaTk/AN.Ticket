﻿using AN.Ticket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AN.Ticket.Infra.Data.Mappings;
public class PaymentMap : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ContactId)
            .IsRequired();

        builder.Property(p => p.MonthlyFee)
            .IsRequired();

        builder.Property(p => p.DueDate)
            .IsRequired();

        builder.Property(p => p.Paid)
            .IsRequired();

        builder.HasOne(p => p.Contact)
            .WithMany()
            .HasForeignKey(p => p.ContactId)
            .IsRequired();

        builder.HasOne(p => p.PaymentPlan)
            .WithMany()
            .HasForeignKey(p => p.PaymentPlanId)
            .IsRequired();

        builder.ToTable("Payments");
    }
}
