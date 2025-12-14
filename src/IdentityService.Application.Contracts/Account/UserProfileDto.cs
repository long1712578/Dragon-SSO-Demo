namespace IdentityService.Application.Contracts.Account;

/// <summary>
/// User profile with types, features, roles for backend consumption
/// </summary>
public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User types/categories (e.g., "Premium", "Basic", "Admin")
    /// Used in X-User-Types header
    /// </summary>
    public List<string> UserTypes { get; set; } = new();
    
    /// <summary>
    /// Features enabled for this user
    /// </summary>
    public List<string> Features { get; set; } = new();
    
    /// <summary>
    /// Roles assigned to user
    /// </summary>
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Permissions granted (for fine-grained access control)
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
