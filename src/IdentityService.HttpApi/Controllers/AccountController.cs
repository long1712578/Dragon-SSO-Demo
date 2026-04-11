using IdentityService.Application.Contracts.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace IdentityService.HttpApi.Controllers;

[Route("api/auth")]
[ApiController]
public class PublicAccountController : AbpControllerBase
{
    private readonly IAccountAppService _accountAppService;

    public PublicAccountController(IAccountAppService accountAppService)
    {
        _accountAppService = accountAppService;
    }

    /// <summary>
    /// Public registration endpoint (no authentication required)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<RegisterResultDto> RegisterAsync([FromBody] RegisterDto input)
    {
        return await _accountAppService.RegisterAsync(input);
    }

    /// <summary>
    /// Get current user profile with types, features, roles
    /// Used by backend API when X-User-Types header is missing
    /// Requires authentication (Bearer token)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<UserProfileDto> GetProfileAsync()
    {
        return await _accountAppService.GetProfileAsync();
    }
}

