using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.Uow;

namespace IdentityService.Domain.Data;

/// <summary>
/// Seed dữ liệu mặc định cho Identity Service:
/// - Roles: admin, employee
/// - Users: admin (admin@dragonsoft.com / 1q2w3E*)
/// - Scopes: IdentityService, OfficeService, HRMService
/// - OIDC Clients: WebApp_Swagger, WebApp, HRMService_App, CV_Website
/// 
/// Chạy tự động khi app start thông qua ABP DataSeeder.
/// </summary>
public class IdentityServiceDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IAbpApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IdentityUserManager _userManager;
    private readonly IConfiguration _configuration;

    public IdentityServiceDataSeeder(
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IAbpApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IdentityUserManager userManager,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _userManager = userManager;
        _configuration = configuration;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedScopesAsync();
        await SeedClientsAsync();
    }

    // ─────────────────────────────────────────────────────
    // Roles
    // ─────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────
    // Users
    // ─────────────────────────────────────────────────────
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

            (await _userManager.CreateAsync(user, password)).CheckErrors();

            var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpper());
            if (role != null)
            {
                (await _userManager.AddToRoleAsync(user, roleName)).CheckErrors();
            }
        }
    }

    // ─────────────────────────────────────────────────────
    // Scopes — cái "tài nguyên" mà client được phép truy cập
    // ─────────────────────────────────────────────────────
    private async Task SeedScopesAsync()
    {
        await CreateScopeIfNotExistsAsync("IdentityService");
        await CreateScopeIfNotExistsAsync("OfficeService");
        await CreateScopeIfNotExistsAsync("HRMService");
    }

    private async Task CreateScopeIfNotExistsAsync(string name)
    {
        if (await _scopeManager.FindByNameAsync(name) == null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = name,
                DisplayName = name,
                Resources = { name }
            });
        }
    }

    // ─────────────────────────────────────────────────────
    // OIDC Clients — app nào được phép sử dụng SSO
    // ─────────────────────────────────────────────────────
    private async Task SeedClientsAsync()
    {
        // Lấy selfUrl từ config để tạo redirect URI động
        var selfUrl = _configuration["App:SelfUrl"]?.TrimEnd('/') ?? "https://localhost:7001";

        // ┌─────────────────────────────────────────────────┐
        // │ 1. Swagger UI — dùng để test API trực tiếp     │
        // └─────────────────────────────────────────────────┘
        await CreateClientIfNotExistsAsync(
            "WebApp_Swagger",
            OpenIddictConstants.ClientTypes.Public,
            null,
            new[] { OpenIddictConstants.GrantTypes.AuthorizationCode },
            new[] { "IdentityService" },
            redirectUris: new[] { $"{selfUrl}/swagger/oauth2-redirect.html" }
        );

        // ┌─────────────────────────────────────────────────┐
        // │ 2. Web App — client chung cho SPA/mobile        │
        // └─────────────────────────────────────────────────┘
        await CreateClientIfNotExistsAsync(
            "WebApp",
            OpenIddictConstants.ClientTypes.Public,
            null,
            new[] { OpenIddictConstants.GrantTypes.Password, OpenIddictConstants.GrantTypes.RefreshToken },
            new[] { "openid", "profile", "roles", "email", "IdentityService", "OfficeService", "HRMService" }
        );

        // ┌─────────────────────────────────────────────────┐
        // │ 2.1 Dragon PayHub — Dedicated Client           │
        // └─────────────────────────────────────────────────┘
        await CreateClientIfNotExistsAsync(
            "dragon-payhub",
            OpenIddictConstants.ClientTypes.Public,
            null,
            new[] { OpenIddictConstants.GrantTypes.Password, OpenIddictConstants.GrantTypes.RefreshToken },
            new[] { "openid", "profile", "roles", "email", "IdentityService" }
        );

        // ┌─────────────────────────────────────────────────┐
        // │ 3. HRM Service — service-to-service (M2M)      │
        // └─────────────────────────────────────────────────┘
        await CreateClientIfNotExistsAsync(
            "HRMService_App",
            OpenIddictConstants.ClientTypes.Confidential,
            "1q2w3e*",
            new[] { OpenIddictConstants.GrantTypes.ClientCredentials },
            new[] { "IdentityService", "OfficeService" }
        );

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ 4. CV Website — trang CV cá nhân, dùng Authorization Code + PKCE  │
        // │    User phải login SSO mới được xem CV                             │
        // │    Type: Public (SPA chạy trên browser, không giữ secret)         │
        // └─────────────────────────────────────────────────────────────────────┘
        var cvRedirectUris = _configuration["App:CorsOrigins"]?
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim().TrimEnd('/'))
            .Where(o => o.Contains("cv") || o.Contains("localhost:5173") || o.Contains("sample"))
            .SelectMany(origin => new[]
            {
                $"{origin}/callback.html",  // OIDC callback
                $"{origin}/"                // Post-logout redirect
            })
            .ToArray() ?? new[] { "http://localhost:5173/callback.html" };

        // Tách redirect và logout URI
        var redirects = cvRedirectUris.Where(u => u.Contains("callback")).ToArray();
        var logouts = cvRedirectUris.Where(u => !u.Contains("callback")).ToArray();

        await CreateCvWebsiteClientAsync(redirects, logouts);
    }

    /// <summary>
    /// Đăng ký CV_Website OIDC client — đây là core của SSO flow:
    /// 
    /// Flow: User mở CV → chưa login → redirect /connect/authorize 
    ///       → SSO login form → login xong → redirect /callback.html với auth code
    ///       → JS exchange code → access_token → hiển thị CV
    /// </summary>
    private async Task CreateCvWebsiteClientAsync(string[] redirectUris, string[] postLogoutRedirectUris)
    {
        const string clientId = "CV_Website";

        if (await _applicationManager.FindByClientIdAsync(clientId) != null)
            return;

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            DisplayName = "Dragon CV Website",
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit, // Không hỏi consent vì là app nội bộ
        };

        // Grant types
        descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.GrantType}{OpenIddictConstants.GrantTypes.AuthorizationCode}");
        descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.GrantType}{OpenIddictConstants.GrantTypes.RefreshToken}");

        // Scopes — openid + profile cho thông tin user
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.Email);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.Profile);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Scopes.Roles);
        descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.Scope}IdentityService");

        // Response types
        descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.ResponseType}{OpenIddictConstants.ResponseTypes.Code}");

        // Endpoints
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);

        // Redirect URIs
        foreach (var uri in redirectUris)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
            {
                descriptor.RedirectUris.Add(parsedUri);
            }
        }

        // Post-logout redirect URIs
        foreach (var uri in postLogoutRedirectUris)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
            {
                descriptor.PostLogoutRedirectUris.Add(parsedUri);
            }
        }

        await _applicationManager.CreateAsync(descriptor);
    }

    private async Task CreateClientIfNotExistsAsync(
        string clientId,
        string type,
        string? secret,
        string[] grantTypes,
        string[] scopes,
        string[]? redirectUris = null)
    {
        if (await _applicationManager.FindByClientIdAsync(clientId) == null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = type,
                DisplayName = clientId
            };

            if (!string.IsNullOrEmpty(secret))
            {
                descriptor.ClientSecret = secret;
            }

            foreach (var grant in grantTypes)
            {
                descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.GrantType}{grant}");
            }

            foreach (var scope in scopes)
            {
                if (new[] { "openid", "profile", "roles", "email", "address", "phone" }.Contains(scope))
                {
                    descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.Scope}{scope}");
                }
                else
                {
                    descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.Scope}{scope}");
                }
            }

            // Đảm bảo các scopes chuẩn luôn được phép (Fix "specified scope not allowed")
            var standardScopes = new[] { 
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}{OpenIddictConstants.Scopes.OpenId}",
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}{OpenIddictConstants.Scopes.Profile}",
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}{OpenIddictConstants.Scopes.Roles}",
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}{OpenIddictConstants.Scopes.Email}"
            };
            foreach (var s in standardScopes) {
                if (!descriptor.Permissions.Contains(s)) descriptor.Permissions.Add(s);
            }

            if (redirectUris != null)
            {
                foreach (var uri in redirectUris)
                {
                    descriptor.RedirectUris.Add(new Uri(uri));
                }
            }

            // Add required endpoints permissions
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);

            if (type == OpenIddictConstants.ClientTypes.Public)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            }

            await _applicationManager.CreateAsync(descriptor);
        }
    }
}
