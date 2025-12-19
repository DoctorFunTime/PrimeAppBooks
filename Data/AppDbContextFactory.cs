using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PrimeAppBooks.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseNpgsql(AppConfig.ConnectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}