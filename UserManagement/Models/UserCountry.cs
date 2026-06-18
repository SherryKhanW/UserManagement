namespace UserManagement.Models;

public class UserCountry : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
}