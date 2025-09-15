using Domain.Entities.Abstract;
using Domain.Entities.Implement;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Persistence;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext()
    {
        
    }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    public DbSet<DomainUser> DomainUsers { get; set; }
    private static string ConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.db.json", optional: false, reloadOnChange: true)
            .Build();
        return configuration.GetConnectionString("DefaultConnection") ?? throw new NullReferenceException("ConnectionString not found");
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(ConnectionString(), options =>
            {
                options.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        }
        optionsBuilder.UseLazyLoadingProxies();
        optionsBuilder
            .UseAsyncSeeding(async (ctx, _, cancellationToken) =>
            {
                var roles = new[] { "Admin", "User" };
                foreach (var role in roles)
                {
                    if (!await ctx.Set<IdentityRole>().AnyAsync(r => r.Name == role, cancellationToken))
                    {
                        await ctx.Set<IdentityRole>().AddAsync(new IdentityRole(role), cancellationToken);
                    }
                }

                await ctx.SaveChangesAsync(cancellationToken);
            })
            .UseSeeding((ctx, _) =>
            {
                var roles = new[] { "Admin", "User" };
                foreach (var role in roles)
                {
                    if (!ctx.Set<IdentityRole>().Any(r => r.Name == role))
                    {
                        ctx.Set<IdentityRole>().Add(new IdentityRole(role));
                    }
                }
                ctx.SaveChanges();
            });
    }

    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        foreach (var entry in entries)
        {
            if (entry.Entity is IdentityUser user && entry.State == EntityState.Added)
            {
                var userId = user.Id;
                var userRole = Roles.FirstOrDefault(r => r.Name == "User");
                var roleId = userRole!.Id;
                UserRoles.Add(new IdentityUserRole<string>
                {
                    UserId = userId,
                    RoleId = roleId // Assigning default role, you can modify this logic as needed
                });
            }

            if (entry.Entity is BaseEntity)
            {
                var entity = entry.Entity as BaseEntity;
                if (entry.State == EntityState.Added)
                {
                    entity!.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity!.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        foreach (var entry in entries)
        {
            if (entry.Entity is IdentityUser user && entry.State == EntityState.Added)
            {
                var userId = user.Id;
                var userRole = await Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
                var roleId = userRole!.Id;
                await UserRoles.AddAsync(new IdentityUserRole<string>
                {
                    UserId = userId,
                    RoleId = roleId // Assigning default role, you can modify this logic as needed
                }, cancellationToken);
            }
            if (entry.Entity is BaseEntity)
            {
                var entity = entry.Entity as BaseEntity;
                if (entry.State == EntityState.Added)
                {
                    entity!.CreatedAt = DateTime.UtcNow;
                    entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity!.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        return await SaveChangesAsync(cancellationToken);
    }
}
