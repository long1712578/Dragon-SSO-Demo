using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using IdentityService.EntityFrameworkCore; // Tham chiếu đến IdentityServiceDbContext

namespace IdentityService.API;

public static class SeedDataExtensions
{
    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        // 1. Chạy Entity Framework Core Migration để tạo cấu trúc bảng (AbpUsers v.v..)
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>();
        await dbContext.Database.MigrateAsync();

        // 2. Seed dữ liệu mặc định của ABP
        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedAsync();
    }
}
