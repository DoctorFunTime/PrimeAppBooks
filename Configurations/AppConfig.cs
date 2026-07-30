using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Configurations
{
    public class AppConfig
    {
        private static readonly IConfigurationRoot Configuration;
        private static string _currentConnectionName = "DefaultConnection";
        public static string SessionTokenSecret => Configuration["Security:SessionTokenSecret"] ?? string.Empty;

        static AppConfig()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static void SwitchConnectionString(string connectionName)
        {
            var validConnections = new[]
            {
                "DefaultConnection",
                "PrimaryConnection",
                "DefaultConnectionTest",
                "DeployedDebugConn",
                "DeployedDebugConnV18",
                "DefaultConnectionV18"
            };

            if (validConnections.Contains(connectionName))
            {
                _currentConnectionName = connectionName;
            }
            else
            {
                throw new ArgumentException($"Invalid connection name. Valid options are: {string.Join(", ", validConnections)}");
            }
        }

        public static string ConnectionString => GetConnectionString(_currentConnectionName);

        public static string GetConnectionString(string connectionName)
        {
            return Configuration.GetConnectionString(connectionName) ??
                   throw new ArgumentException($"Connection string '{connectionName}' not found");
        }

        // Optional: Add properties to get specific connections directly
        public static string DefaultConnection => Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        public static string PrimaryConnection => Configuration.GetConnectionString("PrimaryConnection") ?? string.Empty;
        public static string DefaultConnectionTest => Configuration.GetConnectionString("DefaultConnectionTest") ?? string.Empty;
        public static string DeployedDebugConn => Configuration.GetConnectionString("DeployedDebugConn") ?? string.Empty;
        public static string DeployedDebugConnV18 => Configuration.GetConnectionString("DeployedDebugConnV18") ?? string.Empty;
    }
}