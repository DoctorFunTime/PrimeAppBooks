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

        static AppConfig()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string ConnectionString => GetConnectionString(_currentConnectionName);

        public static string GetConnectionString(string connectionName)
        {
            return Configuration.GetConnectionString(connectionName) ??
                   throw new ArgumentException($"Connection string '{connectionName}' not found");
        }

        // Optional: Add properties to get specific connections directly
        public static string DefaultConnection => Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }
}