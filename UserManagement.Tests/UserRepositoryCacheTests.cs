using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UserManagement.Data;
using UserManagement.Repositories;
using System.Text.Json;
using UserManagement.Models;

namespace UserManagement.Tests;

public class UserRepositoryCacheTests
{
    private AppDbContext _context;
    private UserRepository _repository;
    private IConnectionMultiplexer _redis;
    private IDatabase _cache;
    private ElasticsearchClient _elasticsearch;

    [SetUp]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        _cache = _redis.GetDatabase();

        await _cache.KeyDeleteAsync("User:1");
        await _cache.KeyDeleteAsync("User:all");

        _elasticsearch = new ElasticsearchClient(
            new ElasticsearchClientSettings(new Uri("http://localhost:9200")));

        _repository = new UserRepository(_context, _redis, _elasticsearch);
    }
    
    [Test]
    public async Task GetByIdAsync_ShouldReturnUserFromRedis_WhenUserExistsInCache()
    {
       
        var cache = _redis.GetDatabase();

        var user = new User
        {
            Id = 1,
            FirstName = "Sheharyar",
            LastName = "Khan",
            Email = "sheharyar@test.com"
        };

        var serializedUser = JsonSerializer.Serialize(user);

        await cache.StringSetAsync("User:1", serializedUser);

  
        var result = await _repository.GetByIdAsync(1);

   
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FirstName, Is.EqualTo("Sheharyar"));
    }
    
    [Test]
    public async Task GetByIdAsync_ShouldGetFromDatabaseAndSaveToRedis_WhenCacheIsEmpty()
    {

        var user = new User
        {
            FirstName = "Sheharyar",
            LastName = "Khan",
            Email = "sheharyar@test.com",
            DateOfBirth = new DateTime(2002, 1, 1)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var cacheKey = $"User:{user.Id}";

        await _cache.KeyDeleteAsync(cacheKey);

 
        var result = await _repository.GetByIdAsync(user.Id);


        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FirstName, Is.EqualTo("Sheharyar"));

        var cachedData = await _cache.StringGetAsync(cacheKey);

        Assert.That(cachedData.HasValue, Is.True);
    }
    
    [Test]
    public async Task CreateAsync_ShouldInvalidateGetAllCache()
    {
        await _cache.StringSetAsync("User:all", "cached-users");

        var user = new User
        {
            FirstName = "Sheharyar",
            LastName = "Khan",
            Email = "sheharyar@test.com",
            DateOfBirth = new DateTime(2002, 1, 1)
        };


        await _repository.CreateAsync(user);


        Assert.That(await _cache.KeyExistsAsync("User:all"), Is.False);
    }
    
    [Test]
    public async Task UpdateAsync_ShouldInvalidateCache()
    {
        var user = new User
        {
            FirstName = "Old",
            LastName = "Name",
            Email = "old@test.com",
            DateOfBirth = new DateTime(2002, 1, 1)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _cache.StringSetAsync($"User:{user.Id}", "cached-user");
        await _cache.StringSetAsync("User:all", "cached-users");

        user.FirstName = "New";

        await _repository.UpdateAsync(user);
        
        Assert.That(await _cache.KeyExistsAsync($"User:{user.Id}"), Is.False);
        Assert.That(await _cache.KeyExistsAsync("User:all"), Is.False);
    }
    
    [Test]
    public async Task DeleteAsync_ShouldInvalidateCache()
    {
        var user = new User
        {
            FirstName = "Delete",
            LastName = "Me",
            Email = "delete@test.com",
            DateOfBirth = new DateTime(2002, 1, 1)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _cache.StringSetAsync($"User:{user.Id}", "cached-user");
        await _cache.StringSetAsync("User:all", "cached-users");
        
        await _repository.DeleteAsync(user.Id);
        
        Assert.That(await _cache.KeyExistsAsync($"User:{user.Id}"), Is.False);
        Assert.That(await _cache.KeyExistsAsync("User:all"), Is.False);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await _cache.KeyDeleteAsync("User:1");
        await _cache.KeyDeleteAsync("User:all");

        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();

        await _redis.DisposeAsync();
    }
}