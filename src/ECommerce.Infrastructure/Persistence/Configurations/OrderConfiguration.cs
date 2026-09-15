using ECommerce.Infrastructure.Identity;
using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "Orders",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Orders_CustomerEmail_Valid",
                    "length(trim(\"CustomerEmail\")) BETWEEN 3 AND 254");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_Status_Valid",
                    "\"Status\" IN (1, 2, 3, 4, 5, 6)");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_Currency_Valid",
                    "length(\"Currency\") = 3 AND " +
                    "\"Currency\" = upper(\"Currency\")");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_TotalAmount_Positive",
                    "CAST(\"TotalAmount\" AS NUMERIC) > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_IdempotencyData_Complete",
                    "(\"IdempotencyKey\" IS NULL AND " +
                    "\"RequestFingerprint\" IS NULL) OR " +
                    "(\"IdempotencyKey\" IS NOT NULL AND " +
                    "\"RequestFingerprint\" IS NOT NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_IdempotencyKey_Valid",
                    "\"IdempotencyKey\" IS NULL OR " +
                    $"length(\"IdempotencyKey\") BETWEEN " +
                    $"{Order.IdempotencyKeyMinLength} AND " +
                    $"{Order.IdempotencyKeyMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_RequestFingerprint_Valid",
                    "\"RequestFingerprint\" IS NULL OR " +
                    $"(length(\"RequestFingerprint\") = " +
                    $"{Order.RequestFingerprintLength} AND " +
                    "\"RequestFingerprint\" = upper(\"RequestFingerprint\"))");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingAddress_Complete",
                    "(\"ShippingRecipientFullName\" IS NULL AND " +
                    "\"ShippingPhoneNumber\" IS NULL AND " +
                    "\"ShippingAddressLine1\" IS NULL AND " +
                    "\"ShippingAddressLine2\" IS NULL AND " +
                    "\"ShippingDistrict\" IS NULL AND " +
                    "\"ShippingCity\" IS NULL AND " +
                    "\"ShippingPostalCode\" IS NULL AND " +
                    "\"ShippingCountryCode\" IS NULL) OR " +
                    "(\"ShippingRecipientFullName\" IS NOT NULL AND " +
                    "\"ShippingPhoneNumber\" IS NOT NULL AND " +
                    "\"ShippingAddressLine1\" IS NOT NULL AND " +
                    "\"ShippingDistrict\" IS NOT NULL AND " +
                    "\"ShippingCity\" IS NOT NULL AND " +
                    "\"ShippingPostalCode\" IS NOT NULL AND " +
                    "\"ShippingCountryCode\" IS NOT NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingRecipientFullName_Valid",
                    "\"ShippingRecipientFullName\" IS NULL OR " +
                    "length(trim(\"ShippingRecipientFullName\")) BETWEEN " +
                    $"{OrderAddressSnapshot.RecipientFullNameMinLength} AND " +
                    $"{OrderAddressSnapshot.RecipientFullNameMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingPhoneNumber_Valid",
                    "\"ShippingPhoneNumber\" IS NULL OR " +
                    "length(trim(\"ShippingPhoneNumber\")) BETWEEN " +
                    $"{OrderAddressSnapshot.PhoneNumberMinLength} AND " +
                    $"{OrderAddressSnapshot.PhoneNumberMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingAddressLine1_Valid",
                    "\"ShippingAddressLine1\" IS NULL OR " +
                    "length(trim(\"ShippingAddressLine1\")) BETWEEN " +
                    $"{OrderAddressSnapshot.AddressLine1MinLength} AND " +
                    $"{OrderAddressSnapshot.AddressLine1MaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingAddressLine2_Valid",
                    "\"ShippingAddressLine2\" IS NULL OR " +
                    "length(trim(\"ShippingAddressLine2\")) BETWEEN 1 AND " +
                    $"{OrderAddressSnapshot.AddressLine2MaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingDistrict_Valid",
                    "\"ShippingDistrict\" IS NULL OR " +
                    "length(trim(\"ShippingDistrict\")) BETWEEN " +
                    $"{OrderAddressSnapshot.DistrictMinLength} AND " +
                    $"{OrderAddressSnapshot.DistrictMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingCity_Valid",
                    "\"ShippingCity\" IS NULL OR " +
                    "length(trim(\"ShippingCity\")) BETWEEN " +
                    $"{OrderAddressSnapshot.CityMinLength} AND " +
                    $"{OrderAddressSnapshot.CityMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingPostalCode_Valid",
                    "\"ShippingPostalCode\" IS NULL OR " +
                    "length(trim(\"ShippingPostalCode\")) BETWEEN " +
                    $"{OrderAddressSnapshot.PostalCodeMinLength} AND " +
                    $"{OrderAddressSnapshot.PostalCodeMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_ShippingCountryCode_Valid",
                    "\"ShippingCountryCode\" IS NULL OR " +
                    $"(length(\"ShippingCountryCode\") = " +
                    $"{OrderAddressSnapshot.CountryCodeLength} AND " +
                    "\"ShippingCountryCode\" = upper(\"ShippingCountryCode\"))");
            });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.CustomerEmail)
            .IsRequired()
            .HasMaxLength(Order.MaxCustomerEmailLength);

        builder.Property(order => order.Status)
            .HasConversion<int>();

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(order => order.Currency)
            .IsRequired()
            .HasMaxLength(Order.CurrencyLength)
            .IsFixedLength()
            .HasDefaultValue("TRY");

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.Property(order => order.IdempotencyKey)
            .HasMaxLength(Order.IdempotencyKeyMaxLength);

        builder.Property(order => order.RequestFingerprint)
            .HasMaxLength(Order.RequestFingerprintLength)
            .IsFixedLength();

        builder.OwnsOne(
            order => order.ShippingAddress,
            shippingAddress =>
            {
                shippingAddress.Property(address => address.RecipientFullName)
                    .HasColumnName("ShippingRecipientFullName")
                    .HasMaxLength(
                        OrderAddressSnapshot.RecipientFullNameMaxLength);

                shippingAddress.Property(address => address.PhoneNumber)
                    .HasColumnName("ShippingPhoneNumber")
                    .HasMaxLength(
                        OrderAddressSnapshot.PhoneNumberMaxLength);

                shippingAddress.Property(address => address.AddressLine1)
                    .HasColumnName("ShippingAddressLine1")
                    .HasMaxLength(
                        OrderAddressSnapshot.AddressLine1MaxLength);

                shippingAddress.Property(address => address.AddressLine2)
                    .HasColumnName("ShippingAddressLine2")
                    .HasMaxLength(
                        OrderAddressSnapshot.AddressLine2MaxLength);

                shippingAddress.Property(address => address.District)
                    .HasColumnName("ShippingDistrict")
                    .HasMaxLength(
                        OrderAddressSnapshot.DistrictMaxLength);

                shippingAddress.Property(address => address.City)
                    .HasColumnName("ShippingCity")
                    .HasMaxLength(OrderAddressSnapshot.CityMaxLength);

                shippingAddress.Property(address => address.PostalCode)
                    .HasColumnName("ShippingPostalCode")
                    .HasMaxLength(OrderAddressSnapshot.PostalCodeMaxLength);

                shippingAddress.Property(address => address.CountryCode)
                    .HasColumnName("ShippingCountryCode")
                    .HasMaxLength(OrderAddressSnapshot.CountryCodeLength)
                    .IsFixedLength();
            });

        builder.Navigation(order => order.ShippingAddress)
            .IsRequired(false);

        builder.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(order => order.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<ApplicationUser>()
            .WithMany(customer => customer.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => new
        {
            order.CustomerId,
            order.CreatedAtUtc
        });

        builder.HasIndex(order => new
        {
            order.CustomerId,
            order.IdempotencyKey
        })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}
