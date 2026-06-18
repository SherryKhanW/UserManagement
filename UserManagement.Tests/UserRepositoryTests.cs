using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Repositories;
using StackExchange.Redis;
namespace UserManagement.Tests;
using Elastic.Clients.Elasticsearch;

public class UserRepositoryTests
{
    private AppDbContext _context;
    private UserRepository _repository;
    private IConnectionMultiplexer _redis;
    private ElasticsearchClient _elasticsearch;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _redis = ConnectionMultiplexer.Connect("localhost:6379");
        
        _elasticsearch = new ElasticsearchClient(
            new ElasticsearchClientSettings(new Uri("http://localhost:9200")));
        
        _repository = new UserRepository(_context, _redis, _elasticsearch);
    }
    
    [Test]
    public async Task CreateAsync_ShouldAddUser()
    {
        var user = new User
        {
            FirstName = "Sheharyar",
            LastName = "Khan",
            Email = "sheharyar@test.com",
            PhoneNumber = "123456789",
            DateOfBirth = new DateTime(2002, 1, 1),
            AccountBalance = 1000m,
            LoginCount = 0,
            ReputationScore = 4.8,
            Role = UserRole.User,
            IsActive = true
        };

        var createdUser = await _repository.CreateAsync(user);

        Assert.That(createdUser.Id, Is.GreaterThan(0));
        Assert.That(createdUser.FirstName, Is.EqualTo("Sheharyar"));
        Assert.That(createdUser.Email, Is.EqualTo("sheharyar@test.com"));
        Assert.That(_context.Users.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        var user1 = new User
        {
            FirstName = "User",
            LastName = "One",
            Email = "user1@test.com",
            DateOfBirth = new DateTime(2000, 1, 1)
        };

        var user2 = new User
        {
            FirstName = "User",
            LastName = "Two",
            Email = "user2@test.com",
            DateOfBirth = new DateTime(2001, 1, 1)
        };

        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);

        var users = await _repository.GetAllAsync();

        Assert.That(users.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        var user = new User
        {
            FirstName = "Sheharyar",
            LastName = "Khan",
            Email = "sheharyar@test.com",
            DateOfBirth = new DateTime(2002, 1, 1)
        };

        var createdUser = await _repository.CreateAsync(user);

        var foundUser = await _repository.GetByIdAsync(createdUser.Id);

        Assert.That(foundUser, Is.Not.Null);
        Assert.That(foundUser!.Id, Is.EqualTo(createdUser.Id));
        Assert.That(foundUser.Email, Is.EqualTo("sheharyar@test.com"));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        var user = new User
        {
            FirstName = "User",
            LastName = "Khan",
            Email = "user@test.com",
            DateOfBirth = new DateTime(2002, 1, 1)
        };

        var createdUser = await _repository.CreateAsync(user);


        createdUser.FirstName = "Sheharyar";

        await _repository.UpdateAsync(createdUser);
        
        var updatedUser = await _repository.GetByIdAsync(createdUser.Id);

        Assert.That(updatedUser, Is.Not.Null);
        Assert.That(updatedUser!.FirstName, Is.EqualTo("Sheharyar"));
    }

    [Test]
    public async Task DeleteAsync_ShouldDeleteUser()
    {
        var user = new User
        {
            FirstName = "User",
            LastName = "Khan",
            Email = "sherry@gmail.com"
        };
        
        var createdUser = await _repository.CreateAsync(user);
        await _repository.DeleteAsync(createdUser.Id);
        var deletedUser = await _repository.GetByIdAsync(createdUser.Id);
        Assert.That(deletedUser, Is.Null);
    }
    
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        
        _redis.Dispose();
    }
}
