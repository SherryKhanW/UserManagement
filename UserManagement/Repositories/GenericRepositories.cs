using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using System.Text.Json;
using StackExchange.Redis;
using Elastic.Clients.Elasticsearch;
using UserManagement.Logs;

namespace UserManagement.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly IDatabase _cache;
    protected readonly ElasticsearchClient _elasticsearch;
    
    public GenericRepository(
        AppDbContext context,
        IConnectionMultiplexer redis,
        ElasticsearchClient elasticsearch)
    {
        _context = context;
        _dbSet = _context.Set<T>();
        _cache = redis.GetDatabase();
        _elasticsearch = elasticsearch;
    }
    
    private static string GetAllCacheKey()
    {
        return $"{typeof(T).Name}:all";
    }

    private static string GetByIdCacheKey(int id)
    {
        return $"{typeof(T).Name}:{id}";
    }
    
    private static int? GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");

        if (idProperty == null)
        {
            return null;
        }

        return idProperty.GetValue(entity) as int?;
    }
    
    private static string GetLogDataStreamName()
    {
        return $"logs-{typeof(T).Name.ToLower()}-repository";
    }
    
    private async Task LogAsync(
        string methodName,
        string operation,
        bool success,
        string message,
        int? entityId = null,
        string? errorMessage = null)
    {
        var log = new RepositoryLog
        {
            RepositoryName = $"{typeof(T).Name}Repository",
            EntityName = typeof(T).Name,
            MethodName = methodName,
            Operation = operation,
            Success = success,
            Message = message,
            EntityId = entityId,
            ErrorMessage = errorMessage
        };

        await _elasticsearch.IndexAsync(log, index: GetLogDataStreamName());
    }
    
    public async Task<List<T>> GetAllAsync()
    {
        try
        {
            var cacheKey = GetAllCacheKey();

            var cachedData = await _cache.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                await LogAsync(
                    methodName: nameof(GetAllAsync),
                    operation: "Read",
                    success: true,
                    message: $"{typeof(T).Name} collection retrieved from Redis cache.");

                return JsonSerializer.Deserialize<List<T>>(cachedData.ToString()) ?? [];
            }

            var entities = await _dbSet.ToListAsync();

            var serializedEntities = JsonSerializer.Serialize(entities);

            await _cache.StringSetAsync(
                cacheKey,
                serializedEntities,
                TimeSpan.FromMinutes(10));

            await LogAsync(
                methodName: nameof(GetAllAsync),
                operation: "Read",
                success: true,
                message: $"{typeof(T).Name} collection retrieved from database and cached.");

            return entities;
        }
        catch (Exception ex)
        {
            await LogAsync(
                methodName: nameof(GetAllAsync),
                operation: "Read",
                success: false,
                message: $"Failed to retrieve {typeof(T).Name} collection.",
                errorMessage: ex.Message);

            throw;
        }
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            var cacheKey = GetByIdCacheKey(id);

            var cachedData = await _cache.StringGetAsync(cacheKey);

            if (cachedData.HasValue)
            {
                await LogAsync(
                    methodName: nameof(GetByIdAsync),
                    operation: "Read",
                    success: true,
                    message: $"{typeof(T).Name} retrieved from Redis cache.",
                    entityId: id);

                return JsonSerializer.Deserialize<T>(cachedData.ToString());
            }

            var entity = await _dbSet.FindAsync(id);

            if (entity is not null)
            {
                var serializedEntity = JsonSerializer.Serialize(entity);

                await _cache.StringSetAsync(
                    cacheKey,
                    serializedEntity,
                    TimeSpan.FromMinutes(10));
            }

            await LogAsync(
                methodName: nameof(GetByIdAsync),
                operation: "Read",
                success: true,
                message: entity is null
                    ? $"{typeof(T).Name} with Id {id} not found."
                    : $"{typeof(T).Name} retrieved from database and cached.",
                entityId: id);

            return entity;
        }
        catch (Exception ex)
        {
            await LogAsync(
                methodName: nameof(GetByIdAsync),
                operation: "Read",
                success: false,
                message: $"Failed to retrieve {typeof(T).Name} with Id {id}.",
                entityId: id,
                errorMessage: ex.Message);

            throw;
        }
    }
    

    public async Task<T> CreateAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();

            await _cache.KeyDeleteAsync(GetAllCacheKey());

            await LogAsync(
                methodName: nameof(CreateAsync),
                operation: "Create",
                success: true,
                message: $"{typeof(T).Name} created successfully.",
                entityId: GetEntityId(entity));

            return entity;
        }
        catch (Exception ex)
        {
            await LogAsync(
                methodName: nameof(CreateAsync),
                operation: "Create",
                success: false,
                message: $"Failed to create {typeof(T).Name}.",
                entityId: GetEntityId(entity),
                errorMessage: ex.Message);

            throw;
        }
    }

    public async Task<T> UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();

            var id = GetEntityId(entity);

            if (id is not null)
            {
                await _cache.KeyDeleteAsync(GetByIdCacheKey(id.Value));
            }

            await _cache.KeyDeleteAsync(GetAllCacheKey());

            await LogAsync(
                methodName: nameof(UpdateAsync),
                operation: "Update",
                success: true,
                message: $"{typeof(T).Name} updated successfully.",
                entityId: id);

            return entity;
        }
        catch (Exception ex)
        {
            await LogAsync(
                methodName: nameof(UpdateAsync),
                operation: "Update",
                success: false,
                message: $"Failed to update {typeof(T).Name}.",
                entityId: GetEntityId(entity),
                errorMessage: ex.Message);

            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity == null)
            {
                await LogAsync(
                    methodName: nameof(DeleteAsync),
                    operation: "Delete",
                    success: false,
                    message: $"{typeof(T).Name} with Id {id} not found.",
                    entityId: id);

                return false;
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();

            await _cache.KeyDeleteAsync(GetByIdCacheKey(id));
            await _cache.KeyDeleteAsync(GetAllCacheKey());

            await LogAsync(
                methodName: nameof(DeleteAsync),
                operation: "Delete",
                success: true,
                message: $"{typeof(T).Name} deleted successfully.",
                entityId: id);

            return true;
        }
        catch (Exception ex)
        {
            await LogAsync(
                methodName: nameof(DeleteAsync),
                operation: "Delete",
                success: false,
                message: $"Failed to delete {typeof(T).Name}.",
                entityId: id,
                errorMessage: ex.Message);

            throw;
        }
    }
}