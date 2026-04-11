using System;
using System.Threading.Tasks;
using IdentityService.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace IdentityService.Domain.Repositories;

public interface IDeviceAuthRepository : IRepository<DeviceAuth, Guid>
{
    Task<DeviceAuth?> FindByDeviceIdAsync(string deviceId);
    
    Task<DeviceAuth?> FindByDeviceIdAndUserIdAsync(string deviceId, Guid userId);
    
    Task<DeviceAuth?> FindByAuthTokenAsync(string authToken);
}
