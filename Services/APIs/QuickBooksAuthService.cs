using PrimeAppBooks.Models.APIs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services.APIs
{
    public class QuickBooksAuthService
    {
        private const string ClientId = "ABgUPq0G2mmtHNrOijNNhnRaFaSsPDWTBCS9MapcSz4vS7kTTK";
        private const string ClientSecret = "E4pqBSoi0mBEFsGzrbV6uUnVRq1BU3VqbsmYE1oA";
        private const string RedirectUri = "http://localhost:5000/callback";
        private const string Scope = "com.intuit.quickbooks.accounting";
        private const string AuthUrl = "https://appcenter.intuit.com/connect/oauth2";
        private const string TokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";

        public async Task<TokenResponse> AuthenticateAsync()
        {
            LaunchBrowser();
            var result = await ListenForRedirectAsync();
            return await ExchangeCodeForTokenAsync(result.Code, result.RealmId);
        }

        private void LaunchBrowser()
        {
            var url = $"{AuthUrl}?client_id={ClientId}&response_type=code&scope={Scope}&redirect_uri={RedirectUri}&state=12345";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private async Task<AuthCodeResult> ListenForRedirectAsync()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"{RedirectUri}/");
            listener.Start();

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];
            var realmId = context.Request.QueryString["realmId"];

            listener.Stop();

            return new AuthCodeResult
            {
                Code = code,
                RealmId = realmId
            };
        }

        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, string realmId)
        {
            using var client = new HttpClient();
            var values = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", RedirectUri },
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret }
                };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(TokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            var token = JsonSerializer.Deserialize<TokenResponse>(json);
            token.realmId = realmId; // manually attach it
            return token;
        }
    }
}