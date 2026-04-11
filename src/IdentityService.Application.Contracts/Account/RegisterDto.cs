using System.ComponentModel.DataAnnotations;

namespace IdentityService.Application.Contracts.Account;

public class RegisterDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Device identifier for tracking and 2FA
    /// </summary>
    [StringLength(256)]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Enable two-factor authentication for this user
    /// </summary>
    public bool EnableTwoFactor { get; set; } = false;
}
