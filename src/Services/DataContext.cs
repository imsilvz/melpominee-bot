using Dapper;
using Npgsql;
using System.Data;
namespace Melpominee.Services
{
    public class DataContext
    {
        private static Lazy<DataContext> _instance = new Lazy<DataContext>(() => new DataContext());
        public static DataContext Instance => _instance.Value;

        public DataContext() { }

        public IDbConnection Connect()
        {
            var host = SecretStore.Instance.GetSecret("DB_HOST");
            var user = SecretStore.Instance.GetSecret("DB_USER");
            var password = SecretStore.Instance.GetSecret("DB_PASSWORD");
            var database = SecretStore.Instance.GetSecret("DB_DATABASE");
            return new NpgsqlConnection($"Host={host};Username={user};Password={password};Database={database}");
        }
    }
}
