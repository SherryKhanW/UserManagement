namespace UserManagement.Models;

public class User : BaseEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateTime DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public decimal AccountBalance { get; set; }

    public int LoginCount { get; set; }

    public double ReputationScore { get; set; }

    public UserRole Role { get; set; } = UserRole.User;
    
    public List<UserCountry> UserCountries { get; set; } = [];
}

public enum UserRole
{
    User = 0,
    Admin = 1,
    Support = 2
}