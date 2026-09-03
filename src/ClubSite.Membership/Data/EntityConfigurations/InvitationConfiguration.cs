using ClubSite.Membership.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClubSite.Membership.Data.EntityConfigurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations", "Membership");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(i => i.Token)
            .IsUnique();

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(i => i.Email);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.HasOne(i => i.Member)
            .WithMany()
            .HasForeignKey(i => i.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}