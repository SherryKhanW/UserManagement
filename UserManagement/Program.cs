using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using Elastic.Clients.Elasticsearch;
using UserManagement.Models;
using UserManagement.Repositories;
using ProtoBuf.Grpc.Server;
using UserManagement.Grpc.Contracts;
using UserManagement.Grpc.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString =
        builder.Configuration["Redis:ConnectionString"];

    return ConnectionMultiplexer.Connect(connectionString!);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IRepository<User>, UserRepository>();
builder.Services.AddScoped<IRepository<UserDevice>, UserDeviceRepository>();
builder.Services.AddScoped<IRepository<UserSession>, UserSessionRepository>();
builder.Services.AddScoped<IRepository<Country>, CountryRepository>();
builder.Services.AddScoped<IRepository<UserCountry>, UserCountryRepository>();

builder.Services.AddSingleton(_ =>
{
    var url = builder.Configuration["Elasticsearch:Url"]!;

    var settings = new ElasticsearchClientSettings(new Uri(url));

    return new ElasticsearchClient(settings);
});

builder.Services.AddCodeFirstGrpc();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5235, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.MapGet("/users", async (IRepository<User> repository) =>
{
    var users = await repository.GetAllAsync();
    return Results.Ok(users);
});

app.MapGet("/", () => "Hello from Waada!");

//Redis application

app.MapGet("/redis-test", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();

    await db.StringSetAsync("test-key", "Redis is connected!");

    var value = await db.StringGetAsync("test-key");

    return Results.Ok(new
    {
        message = value.ToString()
    });
});

app.MapGet("/postgres-test", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        postgresConnected = canConnect
    });
});

app.MapGet("/elasticsearch-test", async (ElasticsearchClient client) =>
{
    var response = await client.InfoAsync();

    return Results.Ok(new
    {
        connected = response.IsValidResponse,
        clusterName = response.ClusterName?.ToString()
    });
});

app.MapGrpcService<UserGrpcService>();

app.Run();