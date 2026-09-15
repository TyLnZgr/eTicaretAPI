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
                    "length(trim(\"Label\")) BETWEEN 1 AND 100");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_RecipientFullName_Valid",
                    "length(trim(\"RecipientFullName\")) BETWEEN 2 AND 200");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_PhoneNumber_Valid",
                    "length(trim(\"PhoneNumber\")) BETWEEN 3 AND 30");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_AddressLine1_Valid",
                    "length(trim(\"AddressLine1\")) BETWEEN 5 AND 300");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_AddressLine2_Valid",
                    "\"AddressLine2\" IS NULL OR length(trim(\"AddressLine2\")) BETWEEN 1 AND 300");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_District_Valid",
                    "length(trim(\"District\")) BETWEEN 1 AND 100");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_City_Valid",
                    "length(trim(\"City\")) BETWEEN 1 AND 100");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_PostalCode_Valid",
                    "length(trim(\"PostalCode\")) BETWEEN 1 AND 20");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_CountryCode_Valid",
                    "length(\"CountryCode\") = 2 AND \"CountryCode\" = upper(\"CountryCode\")");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerAddresses_IsDefault_Valid",
                    "\"IsDefault\" IN (0, 1)");
            });

        builder.HasKey(address => address.Id);

        builder.Property(address => address.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.RecipientFullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(address => address.PhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(address => address.AddressLine1)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(address => address.AddressLine2)
            .HasMaxLength(300);

        builder.Property(address => address.District)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(address => address.CountryCode)
            .IsRequired()
            .HasMaxLength(2)
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
