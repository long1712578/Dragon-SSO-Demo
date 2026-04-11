# Dragon SSO Service Generator - ABP Framework 9.0.2
# Author: Long Pham
# Date: 2025-11-16

Write-Host "Dragon SSO Identity Service Generator" -ForegroundColor Cyan
Write-Host "ABP Framework 9.0.2 + OpenIddict" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

function Create-Directory {
    param([string]$Path)
    if (!(Test-Path -Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Host "Created: $Path" -ForegroundColor Green
    }
}

function Create-File {
    param([string]$Path, [string]$Content)
    $directory = Split-Path -Path $Path -Parent
    if ($directory) { 
        Create-Directory -Path $directory 
    }
    Set-Content -Path $Path -Value $Content -Encoding UTF8
    Write-Host "Created: $Path" -ForegroundColor Green
}

$baseDir = Get-Location

Write-Host "Creating ABP project structure..." -ForegroundColor Yellow

# Create directories
Create-Directory "$baseDir/src/IdentityService.API"
Create-Directory "$baseDir/src/IdentityService.Application"
Create-Directory "$baseDir/src/IdentityService.Application.Contracts"
Create-Directory "$baseDir/src/IdentityService.Domain/Data"
Create-Directory "$baseDir/src/IdentityService.Domain.Shared"
Create-Directory "$baseDir/src/IdentityService.EntityFrameworkCore"
Create-Directory "$baseDir/src/IdentityService.HttpApi"

Write-Host ""
Write-Host "Generating source files..." -ForegroundColor Yellow

# ===== API Layer =====
$content = @'
using IdentityService.API;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Async(c => c.File("Logs/logs.txt"))
    .WriteTo.Async(c => c.Console())
    .CreateLogger();

try
{
    Log.Information("Starting Dragon SSO - Identity Service");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host
        .AddAppSettingsSecretsJson()
        .UseAutofac()
        .UseSerilog();

    await builder.AddApplicationAsync<IdentityServiceModule>();
    var app = builder.Build();
    await app.InitializeApplicationAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity Service terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}
'@
Create-File "$baseDir/src/IdentityService.API/Program.cs" $content

$content = @'
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.Swashbuckle;

namespace IdentityService.API;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpAccountWebOpenIddictModule)
)]
public class IdentityServiceModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("IdentityService");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseSqlServer();
        });

        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                options.Audience = "IdentityService";
            });

        ConfigureSwagger(context, configuration);
        ConfigureCors(context, configuration);
    }

    private void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                {"IdentityService", "Identity Service API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Dragon SSO - Identity Service API", 
                    Version = "v1",
                    Description = "SSO Authentication with OpenIddict and ABP Framework 9.0.2"
                });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim().TrimEnd('/'))
                            .ToArray() ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();
        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dragon SSO API");
            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("IdentityService");
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();
    }
}
'@
Create-File "$baseDir/src/IdentityService.API/IdentityServiceModule.cs" $content

$content = @'
{
  "App": {
    "SelfUrl": "https://localhost:7001",
    "CorsOrigins": "https://localhost:7000,http://localhost:4200"
  },
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=DragonSSO_Identity;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "AuthServer": {
    "Authority": "https://localhost:7001",
    "RequireHttpsMetadata": "false",
    "SwaggerClientId": "WebApp_Swagger"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
'@
Create-File "$baseDir/src/IdentityService.API/appsettings.json" $content

$content = @'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Async" Version="1.5.0" />
    <PackageReference Include="Volo.Abp.AspNetCore.Mvc" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Autofac" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.AspNetCore.Serilog" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Swashbuckle" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.EntityFrameworkCore.SqlServer" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Identity.Application" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Identity.HttpApi" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Identity.EntityFrameworkCore" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.OpenIddict.EntityFrameworkCore" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Account.Application" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Account.HttpApi" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Account.Web.OpenIddict" Version="9.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IdentityService.EntityFrameworkCore\IdentityService.EntityFrameworkCore.csproj" />
    <ProjectReference Include="..\IdentityService.Application\IdentityService.Application.csproj" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.API/IdentityService.API.csproj" $content

# ===== DOMAIN SHARED =====
$content = @'
namespace IdentityService.Domain.Shared;

public static class IdentityServiceConsts
{
    public const string DbTablePrefix = "App";
    public const string? DbSchema = null;
}
'@
Create-File "$baseDir/src/IdentityService.Domain.Shared/IdentityServiceConsts.cs" $content

$content = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Volo.Abp.Identity.Domain.Shared" Version="9.0.2" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.Domain.Shared/IdentityService.Domain.Shared.csproj" $content

# ===== DOMAIN =====
$content = @'
using Volo.Abp.Domain;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;

namespace IdentityService.Domain;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpOpenIddictDomainModule)
)]
public class IdentityServiceDomainModule : AbpModule
{
}
'@
Create-File "$baseDir/src/IdentityService.Domain/IdentityServiceDomainModule.cs" $content

$content = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Volo.Abp.Identity.Domain" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.OpenIddict.Domain" Version="9.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IdentityService.Domain.Shared\IdentityService.Domain.Shared.csproj" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.Domain/IdentityService.Domain.csproj" $content

# Data Seeder - Simplified version
$content = @'
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.Uow;

namespace IdentityService.Domain.Data;

public class IdentityServiceDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IOpenIddictApplicationRepository _applicationRepository;
    private readonly IOpenIddictScopeRepository _scopeRepository;
    private readonly IPasswordHasher<IdentityUser> _passwordHasher;

    public IdentityServiceDataSeeder(
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IOpenIddictApplicationRepository applicationRepository,
        IOpenIddictScopeRepository scopeRepository,
        IPasswordHasher<IdentityUser> passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _applicationRepository = applicationRepository;
        _scopeRepository = scopeRepository;
        _passwordHasher = passwordHasher;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedScopesAsync();
        await SeedClientsAsync();
    }

    private async Task SeedRolesAsync()
    {
        await CreateRoleIfNotExistsAsync("admin");
        await CreateRoleIfNotExistsAsync("employee");
    }

    private async Task CreateRoleIfNotExistsAsync(string roleName)
    {
        if (await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpper()) == null)
        {
            await _roleRepository.InsertAsync(new IdentityRole(Guid.NewGuid(), roleName, null));
        }
    }

    private async Task SeedUsersAsync()
    {
        await CreateUserIfNotExistsAsync("admin", "admin@dragonsoft.com", "1q2w3E*", "admin");
        await CreateUserIfNotExistsAsync("employee", "employee@dragonsoft.com", "1q2w3E*", "employee");
    }

    private async Task CreateUserIfNotExistsAsync(string userName, string email, string password, string roleName)
    {
        if (await _userRepository.FindByNormalizedUserNameAsync(userName.ToUpper()) == null)
        {
            var user = new IdentityUser(Guid.NewGuid(), userName, email, null);
            user.SetPasswordHash(_passwordHasher.HashPassword(user, password));
            await _userRepository.InsertAsync(user);

            var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpper());
            if (role != null)
            {
                user.AddRole(role.Id);
            }
        }
    }

    private async Task SeedScopesAsync()
    {
        await CreateScopeIfNotExistsAsync("IdentityService");
        await CreateScopeIfNotExistsAsync("OfficeService");
        await CreateScopeIfNotExistsAsync("HRMService");
    }

    private async Task CreateScopeIfNotExistsAsync(string name)
    {
        if (await _scopeRepository.FindByNameAsync(name) == null)
        {
            await _scopeRepository.InsertAsync(new OpenIddictScope
            {
                Name = name,
                DisplayName = name,
                Resources = { name }
            }, autoSave: true);
        }
    }

    private async Task SeedClientsAsync()
    {
        await CreateClientIfNotExistsAsync(
            "WebApp", 
            OpenIddictConstants.ClientTypes.Public,
            null,
            new[] { OpenIddictConstants.GrantTypes.Password, OpenIddictConstants.GrantTypes.RefreshToken },
            new[] { "IdentityService", "OfficeService", "HRMService" }
        );

        await CreateClientIfNotExistsAsync(
            "HRMService_App",
            OpenIddictConstants.ClientTypes.Confidential,
            "1q2w3e*",
            new[] { OpenIddictConstants.GrantTypes.ClientCredentials },
            new[] { "IdentityService", "OfficeService" }
        );
    }

    private async Task CreateClientIfNotExistsAsync(
        string clientId, 
        string type, 
        string? secret,
        string[] grantTypes, 
        string[] scopes)
    {
        if (await _applicationRepository.FindByClientIdAsync(clientId) == null)
        {
            var app = new OpenIddictApplication(Guid.NewGuid())
            {
                ClientId = clientId,
                ClientType = type,
                DisplayName = clientId
            };

            if (!string.IsNullOrEmpty(secret))
            {
                app.ClientSecret = secret;
            }

            foreach (var grant in grantTypes)
            {
                app.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.GrantType}{grant}");
            }

            foreach (var scope in scopes)
            {
                app.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.Scope}{scope}");
            }

            await _applicationRepository.InsertAsync(app, autoSave: true);
        }
    }
}
'@
Create-File "$baseDir/src/IdentityService.Domain/Data/IdentityServiceDataSeeder.cs" $content

# ===== EF CORE =====
$content = @'
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;

namespace IdentityService.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class IdentityServiceDbContext : AbpDbContext<IdentityServiceDbContext>, IIdentityDbContext
{
    public IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
    }
}
'@
Create-File "$baseDir/src/IdentityService.EntityFrameworkCore/IdentityServiceDbContext.cs" $content

$content = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Volo.Abp.EntityFrameworkCore.SqlServer" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.Identity.EntityFrameworkCore" Version="9.0.2" />
    <PackageReference Include="Volo.Abp.OpenIddict.EntityFrameworkCore" Version="9.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IdentityService.Domain\IdentityService.Domain.csproj" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.EntityFrameworkCore/IdentityService.EntityFrameworkCore.csproj" $content

# ===== APPLICATION CONTRACTS =====
$content = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Volo.Abp.Identity.Application.Contracts" Version="9.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IdentityService.Domain.Shared\IdentityService.Domain.Shared.csproj" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.Application.Contracts/IdentityService.Application.Contracts.csproj" $content

# ===== APPLICATION =====
$content = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Volo.Abp.Identity.Application" Version="9.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IdentityService.Domain\IdentityService.Domain.csproj" />
    <ProjectReference Include="..\IdentityService.Application.Contracts\IdentityService.Application.Contracts.csproj" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.Application/IdentityService.Application.csproj" $content

# ===== HTTP API =====
$content = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Volo.Abp.Identity.HttpApi" Version="9.0.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IdentityService.Application.Contracts\IdentityService.Application.Contracts.csproj" />
  </ItemGroup>
</Project>
'@
Create-File "$baseDir/src/IdentityService.HttpApi/IdentityService.HttpApi.csproj" $content

# ===== SOLUTION FILE =====
$content = @'
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.API", "src\IdentityService.API\IdentityService.API.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.Application", "src\IdentityService.Application\IdentityService.Application.csproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.Application.Contracts", "src\IdentityService.Application.Contracts\IdentityService.Application.Contracts.csproj", "{33333333-3333-3333-3333-333333333333}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.Domain", "src\IdentityService.Domain\IdentityService.Domain.csproj", "{44444444-4444-4444-4444-444444444444}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.Domain.Shared", "src\IdentityService.Domain.Shared\IdentityService.Domain.Shared.csproj", "{55555555-5555-5555-5555-555555555555}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.EntityFrameworkCore", "src\IdentityService.EntityFrameworkCore\IdentityService.EntityFrameworkCore.csproj", "{66666666-6666-6666-6666-666666666666}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "IdentityService.HttpApi", "src\IdentityService.HttpApi\IdentityService.HttpApi.csproj", "{77777777-7777-7777-7777-777777777777}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{11111111-1111-1111-1111-111111111111}.Release|Any CPU.Build.0 = Release|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{22222222-2222-2222-2222-222222222222}.Release|Any CPU.Build.0 = Release|Any CPU
		{33333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{33333333-3333-3333-3333-333333333333}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{33333333-3333-3333-3333-333333333333}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{33333333-3333-3333-3333-333333333333}.Release|Any CPU.Build.0 = Release|Any CPU
		{44444444-4444-4444-4444-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{44444444-4444-4444-4444-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{44444444-4444-4444-4444-444444444444}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{44444444-4444-4444-4444-444444444444}.Release|Any CPU.Build.0 = Release|Any CPU
		{55555555-5555-5555-5555-555555555555}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{55555555-5555-5555-5555-555555555555}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{55555555-5555-5555-5555-555555555555}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{55555555-5555-5555-5555-555555555555}.Release|Any CPU.Build.0 = Release|Any CPU
		{66666666-6666-6666-6666-666666666666}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{66666666-6666-6666-6666-666666666666}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{66666666-6666-6666-6666-666666666666}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{66666666-6666-6666-6666-666666666666}.Release|Any CPU.Build.0 = Release|Any CPU
		{77777777-7777-7777-7777-777777777777}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{77777777-7777-7777-7777-777777777777}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{77777777-7777-7777-7777-777777777777}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{77777777-7777-7777-7777-777777777777}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
'@
Create-File "$baseDir/DragonSSO.sln" $content

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "ABP Framework 9.0.2 SSO Service Created!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. dotnet restore" -ForegroundColor White
Write-Host "2. Update connection string in appsettings.json" -ForegroundColor White
Write-Host "3. cd src/IdentityService.API" -ForegroundColor White
Write-Host "4. dotnet ef database update" -ForegroundColor White
Write-Host "5. dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Access Swagger: https://localhost:7001/swagger" -ForegroundColor Yellow