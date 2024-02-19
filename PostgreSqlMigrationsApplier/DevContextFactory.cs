using Core.Database;
using Microsoft.EntityFrameworkCore.Design;

namespace PostgreSqlMigrationsApplier;

public class DevContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        return new DatabaseContext("Host=localhost;Port=5432;Database=SpotifyListenTogether;Username=postgres;Password=postgres;Include Error Detail=true");
    }
}