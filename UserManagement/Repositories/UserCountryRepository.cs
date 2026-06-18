using UserManagement.Data;
using UserManagement.Models;
using StackExchange.Redis;
using Elastic.Clients.Elasticsearch;

namespace UserManagement.Repositories;

public class UserCountryRepository : GenericRepository<UserCountry>
{
    public UserCountryRepository(AppDbContext context, IConnectionMultiplexer redis, ElasticsearchClient elasticsearch) : base(context, redis, elasticsearch)
    {
    }
}