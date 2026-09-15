using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

public sealed class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(
            "Payments",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Payments_Amount_Positive",
                    "CAST(\"Amount\" AS NUMERIC) > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Payments_Currency_Valid",
                    "length(\"Currency\") = 3 AND " +
                    "\"Currency\" = upper(\"Currency\")");

                tableBuilder.HasCheckConstraint(
                    "CK_Payments_IdempotencyKey_Valid",
                    "length(trim(\"IdempotencyKey\")) BETWEEN 8 AND 100");

                tableBuilder.HasCheckConstraint(
                    "CK_Payments_RequestFingerprint_Valid",
                    "length(\"RequestFingerprint\") = 64");

                tableBuilder.HasCheckConstraint(
                    "CK_Payments_Provider_Valid",
                    "length(trim(\"Provider\")) BETWEEN 1 AND 100");

                tableBuilder.HasCheckConstraint(
                    "CK_Payments_Status_Valid",
                    "\"Status\" IN (1, 2, 3)");

                tableBuilder.HasCheckConstraint(
                    "CK_Payments_Result_Consistent",
                    "(\"Status\" = 1 AND \"ProviderPaymentId\" IS NULL " +
                    "AND \"FailureCode\" IS NULL) OR " +
                    "(\"Status\" = 2 AND \"ProviderPaymentId\" IS NOT NULL " +
                    "AND \"FailureCode\" IS NULL) OR " +
                    "(\"Status\" = 3 AND \"FailureCode\" IS NOT NULL)");
            });

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(payment => payment.RequestFingerprint)
            .IsRequired()
            .HasMaxLength(64)
            .IsFixedLength();

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2);

        builder.Property(payment => payment.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(payment => payment.Status)
            .HasConversion<int>();

        builder.Property(payment => payment.Provider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(payment => payment.ProviderPaymentId)
            .HasMaxLength(200);

        builder.Property(payment => payment.FailureCode)
            .HasMaxLength(100);

        builder.Property(payment => payment.CreatedAtUtc)
            .IsRequired();

        builder.Property(payment => payment.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(payment => payment.Order)
            .WithMany(order => order.Payments)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(payment => new
        {
            payment.OrderId,
            payment.IdempotencyKey
        })
            .IsUnique();

        builder.HasIndex(payment => payment.OrderId)
            .HasDatabaseName("UX_Payments_OrderId_Active")
            .HasFilter("\"Status\" IN (1, 2)")
            .IsUnique();

        builder.HasIndex(payment => new
        {
            payment.OrderId,
            payment.CreatedAtUtc
        });
    }
}
