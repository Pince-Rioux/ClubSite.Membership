using ClubSite.Membership.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClubSite.Membership.Data.EntityConfigurations;

public class MembershipFeeConfiguration : IEntityTypeConfiguration<MembershipFee>
{
    public void Configure(EntityTypeBuilder<MembershipFee> builder)
    {
        builder.ToTable("MembershipFee", "Membership");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(f => f.Interval)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(f => f.Category)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(f => f.IsDefault)
            .IsRequired();

        builder.HasMany(f => f.Members)
            .WithOne(m => m.MembershipFee)
            .HasForeignKey(m => m.MembershipFeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}