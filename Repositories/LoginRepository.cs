using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Repositories
{
    public class LoginRepository
    {
        private DbContextOptions<AppDbContext> BuildOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(AppConfig.ConnectionString)
                .Options;
        }

        public User? GetLoginDetails(string username)
        {
            using var context = new AppDbContext(BuildOptions());
            return context.Users.AsNoTracking()
                .FirstOrDefault(u => u.Username == username);
        }
    }
}
