namespace IdentityService.Application.Contracts.Account;

public class RegisterResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
}
