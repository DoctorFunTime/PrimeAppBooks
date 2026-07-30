using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountSurname { get; set; } = string.Empty;
        public string AccountTitle { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string AccountDepartment { get; set; } = string.Empty;
        public bool AccountTasks { get; set; }
    }
}
