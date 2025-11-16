using IdentityService.Domain;
using Volo.Abp.Application;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace IdentityService.Application;

[DependsOn(
    typeof(IdentityServiceDomainModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpIdentityApplicationModule)
)]
public class IdentityServiceApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
      Configure<AbpAutoMapperOptions>(options =>
        {
         options.AddMaps<IdentityServiceApplicationModule>();
        });
    }
}
