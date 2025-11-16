using Volo.Abp.Application.Services;

namespace IdentityService.Application.Contracts.Account;

public interface IAccountAppService : IApplicationService
{
    Task<RegisterResultDto> RegisterAsync(RegisterDto input);
}
