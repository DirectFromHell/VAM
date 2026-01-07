using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using VAM.Models;

namespace VAM.Services
{
    public class AccountStorage
    {
        private readonly string _dataPath;
        private readonly string _settingsPath;
        private List<RiotAccount> _accounts;
        private AppSettings _settings;

        public AccountStorage()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "V-AM");
            Directory.CreateDirectory(appDataPath);
            
            _dataPath = Path.Combine(appDataPath, "accounts.json");
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            
            _accounts = LoadAccounts();
            _settings = LoadSettings();
        }

        private List<RiotAccount> LoadAccounts()
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = File.ReadAllText(_dataPath);
                    return JsonConvert.DeserializeObject<List<RiotAccount>>(json) ?? new List<RiotAccount>();
                }
            }
            catch { }
            return new List<RiotAccount>();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            var accountsJson = JsonConvert.SerializeObject(_accounts, Formatting.Indented);
            File.WriteAllText(_dataPath, accountsJson);

            var settingsJson = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, settingsJson);
        }

        public List<RiotAccount> GetAllAccounts() => _accounts;

        public void AddAccount(RiotAccount account)
        {
            _accounts.Add(account);
            Save();
        }

        public void UpdateAccount(RiotAccount account)
        {
            var index = _accounts.FindIndex(a => a.Id == account.Id);
            if (index >= 0)
            {
                _accounts[index] = account;
                Save();
            }
        }

        public void DeleteAccount(string id)
        {
            _accounts.RemoveAll(a => a.Id == id);
            Save();
        }

        public RiotAccount? GetAccount(string id) => _accounts.Find(a => a.Id == id);

        public AppSettings GetSettings() => _settings;

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            Save();
        }

        public string GetDataPath() => Path.GetDirectoryName(_dataPath) ?? "";

        // Search and Filter methods
        public List<RiotAccount> SearchAccounts(string query)
        {
            query = query.ToLower();
            return _accounts.Where(a => 
                a.Username.ToLower().Contains(query) ||
                (a.DisplayName?.ToLower().Contains(query) ?? false) ||
                (a.Notes?.ToLower().Contains(query) ?? false)
            ).ToList();
        }

        public List<RiotAccount> FilterByRegion(string region)
        {
            return _accounts.Where(a => a.Region?.Equals(region, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        }

        public List<RiotAccount> FilterByStatus(AccountStatus status)
        {
            return _accounts.Where(a => a.Status == status).ToList();
        }

        public List<RiotAccount> FilterByRank(string rank)
        {
            return _accounts.Where(a => a.Rank?.Equals(rank, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        }

        public List<RiotAccount> FilterByGroup(string group)
        {
            return _accounts.Where(a => a.Group?.Equals(group, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        }

        public List<RiotAccount> GetFavorites()
        {
            return _accounts.Where(a => a.IsFavorite).ToList();
        }

        public List<RiotAccount> GetReadyAccounts()
        {
            return _accounts.Where(a => a.IsReadyForDaily && a.Status == AccountStatus.Active).ToList();
        }
    }

    public class AppSettings
    {
        public string RiotClientPath { get; set; } = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
        public string ValorantPath { get; set; } = @"C:\Riot Games\VALORANT\live\VALORANT.exe";
        public bool AutoCloseClient { get; set; } = true;
        public int LoginDelayMs { get; set; } = 2000;
        public string DefaultRegion { get; set; } = "EU";
        public string? HenrikApiKey { get; set; } // Optional API key for Henrik API
        public int QueueDelayMinutes { get; set; } = 5;
    }
}
