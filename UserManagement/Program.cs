using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using Elastic.Clients.Elasticsearch;


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

builder.Services.AddSingleton(_ =>
{
    var url = builder.Configuration["Elasticsearch:Url"]!;

    var settings = new ElasticsearchClientSettings(new Uri(url));

    return new ElasticsearchClient(settings);
});


var app = builder.Build();

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

app.Run();