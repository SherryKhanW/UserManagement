using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<UserCountry> UserCountries { get; set; }
}