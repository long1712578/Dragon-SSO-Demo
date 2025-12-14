using System;
using Volo.Abp.Domain.Entities;

namespace IdentityService.Domain.Entities;

/// <summary>
/// Tracks authenticated devices and their tokens for 2FA and refresh flows
/// </summary>
public class DeviceAuth : Entity<Guid>
{
    /// <summary>
    /// Unique device identifier (from client)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who owns this device
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// OTP/TOTP secret for 2FA (encrypted)
    /// </summary>
    public string? OtpSecret { get; set; }

    /// <summary>
    /// Temporary auth token (20-30s lifetime) used to exchange for access token
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// When AuthToken expires
    /// </summary>
    public DateTime? AuthTokenExpiry { get; set; }

    /// <summary>
    /// Last refresh token issued for this device (hashed)
    /// </summary>
    public string? RefreshTokenHash { get; set; }

    /// <summary>
    /// Device name/info for user reference
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Is this device trusted (skip 2FA)
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// Last time device was used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    protected DeviceAuth()
    {
    }

    public DeviceAuth(
        Guid id,
        string deviceId,
        Guid userId,
        string? deviceName = null) : base(id)
    {
        DeviceId = deviceId;
        UserId = userId;
        DeviceName = deviceName;
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
    }

    public void SetAuthToken(string token, int expirySeconds = 30)
    {
        AuthToken = token;
        AuthTokenExpiry = DateTime.UtcNow.AddSeconds(expirySeconds);
    }

    public bool IsAuthTokenValid()
    {
        return !string.IsNullOrEmpty(AuthToken) &&
               AuthTokenExpiry.HasValue &&
               AuthTokenExpiry.Value > DateTime.UtcNow;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string hashedToken)
    {
        RefreshTokenHash = hashedToken;
        UpdateLastUsed();
    }

    public void SetTrusted(bool trusted)
    {
        IsTrusted = trusted;
    }
}
