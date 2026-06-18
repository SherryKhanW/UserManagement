namespace UserManagement.Models;

public class Country : BaseEntity
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";

    public List<UserCountry> UserCountries { get; set; } = [];
}