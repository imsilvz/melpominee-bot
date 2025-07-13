using Npgsql;

namespace Melpominee.Services;

public class DataContext
{
    private static Lazy<DataContext> _instance = new Lazy<DataContext>(() => new DataContext());
    public static DataContext Instance => _instance.Value;

    private NpgsqlDataSource _dataSource;
    public DataContext()
    {
        _dataSource = NpgsqlDataSource.Create(GetConnectionString());
    }

    ~DataContext()
    {
        _dataSource.Dispose();
    }

    public async Task Initialize()
    {
        Console.WriteLine("Beginning Database Migration...");
        string? last_migration = null;
        await using var conn = await GetConnection();
        await using (var batch = new NpgsqlBatch(conn)
        {
            BatchCommands =
            {
                new("CREATE SCHEMA IF NOT EXISTS melpominee_bot"),
                new
                (
                    @"
                    CREATE TABLE IF NOT EXISTS melpominee_bot.migrations (
                        migration_name TEXT PRIMARY KEY,
                        applied_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                    )"
                ),
                new("SELECT migration_name FROM melpominee_bot.migrations ORDER BY migration_name DESC LIMIT 1")
            }
        })
        await using (var reader = await batch.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                last_migration = reader.GetString(0);
            }
        }

        if (Directory.Exists($"{Directory.GetCurrentDirectory()}/migrations/"))
        {
            var migrationFiles = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/migrations/");
            if (last_migration is not null)
            {
                migrationFiles = migrationFiles
                .Where(file => string.Compare(Path.GetFileName(file), last_migration, StringComparison.Ordinal) > 0)
                .ToArray();
            }
            migrationFiles = migrationFiles.OrderBy(f => f).ToArray();

            foreach (var fileName in migrationFiles)
            {
                string sqlScript = File.ReadAllText(fileName);
                await using (var trans = await conn.BeginTransactionAsync())
                {
                    await using (var cmd = new NpgsqlCommand(sqlScript, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    await using (var cmd = new NpgsqlCommand("INSERT INTO melpominee_bot.migrations VALUES ($1)", conn)
                    {
                        Parameters = {new() {Value = Path.GetFileName(fileName)}}
                    })
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    await trans.CommitAsync();
                    Console.WriteLine($"Migration Applied: {Path.GetFileName(fileName)}");
                }
            }
        }
        Console.WriteLine("Database Migrations Complete.");
    }

    public async Task<NpgsqlConnection> GetConnection()
    {
        return await _dataSource.OpenConnectionAsync();
    }

    private string GetConnectionString()
    {
        var host = SecretStore.Instance.GetSecret("DB_HOST");
        var user = SecretStore.Instance.GetSecret("DB_USER");
        var password = SecretStore.Instance.GetSecret("DB_PASSWORD");
        var database = SecretStore.Instance.GetSecret("DB_DATABASE");
        return $"Host={host};Username={user};Password={password};Database={database}";
    }
}
