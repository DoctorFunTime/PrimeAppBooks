using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Models.APIs
{
    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string realmId { get; set; }
        public int expires_in { get; set; }
    }

    public class AuthCodeResult
    {
        public string Code { get; set; }
        public string RealmId { get; set; }
    }
}