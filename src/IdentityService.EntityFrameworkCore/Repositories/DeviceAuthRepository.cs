using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Repositories;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace IdentityService.EntityFrameworkCore.Repositories;

public class DeviceAuthRepository : EfCoreRepository<IdentityServiceDbContext, DeviceAuth, Guid>,
    IDeviceAuthRepository
{
    public DeviceAuthRepository(IDbContextProvider<IdentityServiceDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<DeviceAuth?> FindByDeviceIdAsync(string deviceId)
    {
        var dbSet = await GetDbSetAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(
            dbSet.Where(x => x.DeviceId == deviceId));
    }

    public async Task<DeviceAuth?> FindByDeviceIdAndUserIdAsync(string deviceId, Guid userId)
    {
        var dbSet = await GetDbSetAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(
            dbSet.Where(x => x.DeviceId == deviceId && x.UserId == userId));
    }

    public async Task<DeviceAuth?> FindByAuthTokenAsync(string authToken)
    {
        var dbSet = await GetDbSetAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(
            dbSet.Where(x => x.AuthToken == authToken && x.AuthTokenExpiry > DateTime.UtcNow));
    }
}
