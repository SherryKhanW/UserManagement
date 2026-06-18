using UserManagement.Data;
using UserManagement.Models;
using StackExchange.Redis;
using Elastic.Clients.Elasticsearch;

namespace UserManagement.Repositories;

public class UserDeviceRepository : GenericRepository<UserDevice>
{
    public UserDeviceRepository(AppDbContext context, IConnectionMultiplexer redis, ElasticsearchClient elasticsearch) : base(context, redis, elasticsearch)
    {
    }
}