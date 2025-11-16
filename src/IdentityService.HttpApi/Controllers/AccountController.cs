using IdentityService.Application.Contracts.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace IdentityService.HttpApi.Controllers;

[Route("api/public-account")]
[ApiController]
public class PublicAccountController : AbpControllerBase
{
    private readonly IAccountAppService _accountAppService;

    public PublicAccountController(IAccountAppService accountAppService)
    {
        _accountAppService = accountAppService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<RegisterResultDto> RegisterAsync([FromBody] RegisterDto input)
    {
        return await _accountAppService.RegisterAsync(input);
    }
}
