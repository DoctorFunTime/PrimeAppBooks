using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services
{
    public static class SessionTokenService
    {
        private static string Secret => Configurations.AppConfig.SessionTokenSecret;

        public static bool TryValidateToken(string? tokenBase64, out string username)
        {
            username = string.Empty;
            if (string.IsNullOrWhiteSpace(tokenBase64)) return false;

            try
            {
                string token = Encoding.UTF8.GetString(Convert.FromBase64String(tokenBase64));
                string[] parts = token.Split('|');
                if (parts.Length != 3) return false;

                string user = parts[0];
                string timestampStr = parts[1];
                string signature = parts[2];

                string payload = $"{user}|{timestampStr}";
                string expectedSignature = Sign(payload);

                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(signature),
                        Encoding.UTF8.GetBytes(expectedSignature)))
                {
                    return false;
                }

                long timestamp = long.Parse(timestampStr);
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now - timestamp > 60) return false;

                username = user;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Sign(string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }
    }
}
