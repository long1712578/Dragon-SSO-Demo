using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace IdentityService.API;

public static class SeedDataExtensions
{
    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedAsync();
  }
}
