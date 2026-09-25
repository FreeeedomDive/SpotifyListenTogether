using Core.Sessions.Storage;
using Core.Spotify.Auth.Storage;
using Microsoft.EntityFrameworkCore;
using SqlRepositoryBase.Core.ContextBuilders;

namespace Core.Database;

public class DatabaseContext : PostgreSqlDbContext
{
    public DatabaseContext(string connectionString) : base(connectionString)
    {
    }

    public DbSet<AuthApiUsersStorageElement> AuthApiUsers { get; set; }
    public DbSet<SessionStorageElement> Sessions { get; set; }
}