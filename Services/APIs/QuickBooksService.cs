using PrimeAppBooks.Models.APIs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services.APIs
{
    public class QuickBooksService
    {
        public async Task<CompanyInfo> GetCompanyInfoAsync(string accessToken, string realmId)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/companyinfo/{realmId}";
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            var wrapper = JsonSerializer.Deserialize<CompanyInfoWrapper>(json);
            return wrapper?.CompanyInfo;
        }
    }
}