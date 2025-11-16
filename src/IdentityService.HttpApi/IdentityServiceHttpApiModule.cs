using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace IdentityService.HttpApi;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule)
)]
public class IdentityServiceHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(IdentityServiceHttpApiModule).Assembly);
        });
    }
}
