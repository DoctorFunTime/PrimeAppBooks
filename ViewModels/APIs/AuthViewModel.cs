using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

namespace PrimeAppBooks.ViewModels.APIs
{
    public partial class AuthViewModel : ObservableObject
    {
        [RelayCommand]
        private async Task AuthenticateAsync()
        {
            var clientId = "YOUR_CLIENT_ID";
            var redirectUri = "http://localhost:5000/callback";
            var scope = "com.intuit.quickbooks.accounting";
            var authUrl = $"https://appcenter.intuit.com/connect/oauth2?client_id={clientId}&response_type=code&scope={scope}&redirect_uri={redirectUri}&state=12345";

            // Launch browser for user login
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        }

        private async Task<string> ListenForRedirectAsync()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/callback/");
            listener.Start();

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            listener.Stop();
            return code;
        }

        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var client = new HttpClient();
            var values = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", "http://localhost:5000/callback" },
                    { "client_id", "YOUR_CLIENT_ID" },
                    { "client_secret", "YOUR_CLIENT_SECRET" }
                };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer", content);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TokenResponse>(json);
        }
    }
}