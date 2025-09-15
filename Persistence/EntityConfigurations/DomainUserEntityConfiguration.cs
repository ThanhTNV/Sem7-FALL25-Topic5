using Microsoft.EntityFrameworkCore;
using Domain.Entities.Implement;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;

namespace Persistence.EntityConfigurations;

public class DomainUserEntityConfiguration : IEntityTypeConfiguration<DomainUser>
{
    public void Configure(EntityTypeBuilder<DomainUser> b)
    {
        b.ToTable("DomainUser");
        b.HasKey(x => x.Id);

        // Config Foreign Key
        b.HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<DomainUser>(x => x.IdentityUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
