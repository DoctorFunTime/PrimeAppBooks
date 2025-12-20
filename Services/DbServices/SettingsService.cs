using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services.DbServices
{
    public class SettingsService
    {
        private readonly AppDbContext _context;
        private static readonly ConcurrentDictionary<string, string> _cache = new();

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            if (_cache.TryGetValue(key, out var cachedValue))
            {
                return cachedValue;
            }

            var setting = await _context.AccountingSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            var value = setting?.SettingValue ?? defaultValue;
            _cache[key] = value;
            return value;
        }

        public async Task<int> GetIntSettingAsync(string key, int defaultValue = 0)
        {
            var value = await GetSettingAsync(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task SetSettingAsync(string key, string value, string description = "")
        {
            var setting = await _context.AccountingSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
            {
                setting = new AccountingSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    Description = description,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.AccountingSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
                if (!string.IsNullOrEmpty(description))
                    setting.Description = description;
                setting.UpdatedAt = DateTime.UtcNow;
                _context.AccountingSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            _cache[key] = value;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public async Task<int> GetBaseCurrencyIdAsync()
        {
            // First check the setting
            var baseCurrencyIdSetting = await GetIntSettingAsync(SettingConstants.BaseCurrencyId, 0);
            if (baseCurrencyIdSetting > 0) return baseCurrencyIdSetting;

            // Fallback to the currency marked as base in the Currencies table
            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.IsBaseCurrency);
            if (baseCurrency != null)
            {
                await SetSettingAsync(SettingConstants.BaseCurrencyId, baseCurrency.CurrencyId.ToString());
                return baseCurrency.CurrencyId;
            }

            return 0;
        }
    }
}
