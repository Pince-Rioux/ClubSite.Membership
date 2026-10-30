using VereinMember.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VereinMember.Data.EntityConfigurations;

public class FamilyGroupConfiguration : IEntityTypeConfiguration<FamilyGroup>
{
    public void Configure(EntityTypeBuilder<FamilyGroup> builder)
    {
        builder.ToTable("FamilyGroup", "Membership");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(200);

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.HasMany(g => g.Members)
            .WithOne(m => m.FamilyGroup)
            .HasForeignKey(m => m.FamilyGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}