using ECommerce.Infrastructure.Identity;
using ECommerce.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class CustomerAddressConfiguration
    : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable(
            "CustomerAddresses",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_Label_Valid",
                    $"length(trim(\"Label\")) BETWEEN " +
                    $"{CustomerAddress.LabelMinLength} AND " +
                    $"{CustomerAddress.LabelMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_RecipientFullName_Valid",
                    $"length(trim(\"RecipientFullName\")) BETWEEN " +
                    $"{CustomerAddress.RecipientFullNameMinLength} AND " +
                    $"{CustomerAddress.RecipientFullNameMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_PhoneNumber_Valid",
                    $"length(trim(\"PhoneNumber\")) BETWEEN " +
                    $"{CustomerAddress.PhoneNumberMinLength} AND " +
                    $"{CustomerAddress.PhoneNumberMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_AddressLine1_Valid",
                    $"length(trim(\"AddressLine1\")) BETWEEN " +
                    $"{CustomerAddress.AddressLine1MinLength} AND " +
                    $"{CustomerAddress.AddressLine1MaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_AddressLine2_Valid",
                    "\"AddressLine2\" IS NULL OR " +
                    "length(trim(\"AddressLine2\")) BETWEEN 1 AND " +
                    CustomerAddress.AddressLine2MaxLength);

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_District_Valid",
                    $"length(trim(\"District\")) BETWEEN " +
                    $"{CustomerAddress.DistrictMinLength} AND " +
                    $"{CustomerAddress.DistrictMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_City_Valid",
                    $"length(trim(\"City\")) BETWEEN " +
                    $"{CustomerAddress.CityMinLength} AND " +
                    $"{CustomerAddress.CityMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_PostalCode_Valid",
                    $"length(trim(\"PostalCode\")) BETWEEN " +
                    $"{CustomerAddress.PostalCodeMinLength} AND " +
                    $"{CustomerAddress.PostalCodeMaxLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_CountryCode_Valid",
                    $"length(\"CountryCode\") = " +
                    $"{CustomerAddress.CountryCodeLength} AND " +
                    "\"CountryCode\" = upper(\"CountryCode\")");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_IsDefault_Valid",
                    "\"IsDefault\" IN (0, 1)");
            });

        builder.HasKey(address => address.Id);

        builder.Property(address => address.Label)
            .IsRequired()
            .HasMaxLength(CustomerAddress.LabelMaxLength);

        builder.Property(address => address.RecipientFullName)
            .IsRequired()
            .HasMaxLength(CustomerAddress.RecipientFullNameMaxLength);

        builder.Property(address => address.PhoneNumber)
            .IsRequired()
            .HasMaxLength(CustomerAddress.PhoneNumberMaxLength);

        builder.Property(address => address.AddressLine1)
            .IsRequired()
            .HasMaxLength(CustomerAddress.AddressLine1MaxLength);

        builder.Property(address => address.AddressLine2)
            .HasMaxLength(CustomerAddress.AddressLine2MaxLength);

        builder.Property(address => address.District)
            .IsRequired()
            .HasMaxLength(CustomerAddress.DistrictMaxLength);

        builder.Property(address => address.City)
            .IsRequired()
            .HasMaxLength(CustomerAddress.CityMaxLength);

        builder.Property(address => address.PostalCode)
            .IsRequired()
            .HasMaxLength(CustomerAddress.PostalCodeMaxLength);

        builder.Property(address => address.CountryCode)
            .IsRequired()
            .HasMaxLength(CustomerAddress.CountryCodeLength)
            .IsFixedLength();

        builder.Property(address => address.CreatedAtUtc)
            .IsRequired();

        builder.Property(address => address.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany(customer => customer.Addresses)
            .HasForeignKey(address => address.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(address => new
        {
            address.CustomerId,
            address.UpdatedAtUtc
        });

        builder.HasIndex(address => address.CustomerId)
            .HasDatabaseName(
                "UX_CustomerAddresses_CustomerId_Default")
            .HasFilter("\"IsDefault\" = 1")
            .IsUnique();
    }
}
