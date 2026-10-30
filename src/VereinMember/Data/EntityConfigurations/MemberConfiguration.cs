using VereinMember.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VereinMember.Data.EntityConfigurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Member", "Membership");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(m => m.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        builder.Property(m => m.Phone)
            .HasMaxLength(50);

        builder.Property(m => m.BirthDate)
            .IsRequired();

        builder.Property(m => m.Street)
            .HasMaxLength(200);

        builder.Property(m => m.City)
            .HasMaxLength(100);

        builder.Property(m => m.ZipCode)
            .HasMaxLength(20);

        builder.Property(m => m.Country)
            .HasMaxLength(100);

        // Encryption via AES-256-GCM (set by MembershipEncryption.Initialize at startup)
        builder.Property(m => m.BankIban)
            .HasMaxLength(512)  // AES-GCM output is larger than input
            .HasConversion(
                v => v == null ? null : MembershipEncryption.Current.Encrypt(v),
                v => v == null ? null : MembershipEncryption.Current.Decrypt(v));

        builder.Property(m => m.BankBic)
            .HasMaxLength(64)
            .HasConversion(
                v => v == null ? null : MembershipEncryption.Current.Encrypt(v),
                v => v == null ? null : MembershipEncryption.Current.Decrypt(v));

        builder.Property(m => m.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(m => m.Notes)
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.ConcurrencyToken)
            .IsRowVersion();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.Property(m => m.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(m => m.Department)
            .WithMany(d => d.Members)
            .HasForeignKey(m => m.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.MembershipFee)
            .WithMany(f => f.Members)
            .HasForeignKey(m => m.MembershipFeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.FirstName, m.LastName });

        // Guardian self-reference (Youth to Adult)
        builder.HasOne(m => m.GuardianMember)
            .WithMany(m => m.DependentMembers)
            .HasForeignKey(m => m.GuardianMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.GuardianMemberId);

        // Family group
        builder.HasOne(m => m.FamilyGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.FamilyGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.FamilyGroupId);
    }
}