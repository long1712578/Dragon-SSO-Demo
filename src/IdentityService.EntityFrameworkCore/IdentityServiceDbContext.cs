using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Authorizations;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;

namespace IdentityService.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class IdentityServiceDbContext : AbpDbContext<IdentityServiceDbContext>, 
    IIdentityDbContext,
    IPermissionManagementDbContext,
    IOpenIddictDbContext
{
    // Identity DbSets
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    
    // PermissionManagement DbSets
    public DbSet<PermissionGrant> PermissionGrants { get; set; }
    public DbSet<PermissionGroupDefinitionRecord> PermissionGroups { get; set; }
    public DbSet<PermissionDefinitionRecord> Permissions { get; set; }
    
    // OpenIddict DbSets
    public DbSet<OpenIddictApplication> Applications { get; set; }
    public DbSet<OpenIddictAuthorization> Authorizations { get; set; }
    public DbSet<OpenIddictScope> Scopes { get; set; }
    public DbSet<OpenIddictToken> Tokens { get; set; }

    // Custom DbSets
    public DbSet<DeviceAuth> DeviceAuths { get; set; }

    public IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigurePermissionManagement();

        // Configure DeviceAuth
        builder.Entity<DeviceAuth>(b =>
        {
            b.ToTable("DeviceAuths");
            b.HasKey(x => x.Id);
            b.Property(x => x.DeviceId).IsRequired().HasMaxLength(256);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.OtpSecret).HasMaxLength(512);
            b.Property(x => x.AuthToken).HasMaxLength(512);
            b.Property(x => x.RefreshTokenHash).HasMaxLength(512);
            b.Property(x => x.DeviceName).HasMaxLength(256);

            b.HasIndex(x => x.DeviceId);
            b.HasIndex(x => new { x.DeviceId, x.UserId });
            b.HasIndex(x => x.AuthToken);
        });
    }
}
