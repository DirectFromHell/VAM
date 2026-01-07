using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using VAM.Models;
using VAM.Services;
using VAM.UI;

namespace VAM
{
    public class Application
    {
        private readonly ConsoleUI _ui;
        private readonly AccountStorage _storage;
        private readonly RiotLauncher _launcher;
        private readonly RiotApiService _apiService;
        private readonly LaunchQueueService _queueService;
        private readonly StatisticsService _statsService;
        private bool _running = true;

        public Application()
        {
            _ui = new ConsoleUI();
            _storage = new AccountStorage();
            _launcher = new RiotLauncher(_storage);
            
            var settings = _storage.GetSettings();
            _apiService = new RiotApiService(settings.HenrikApiKey);
            
            _queueService = new LaunchQueueService(_storage, _launcher);
            _statsService = new StatisticsService(_storage);
        }

        public async Task Run()
        {
            Console.Title = "V-AM | Valorant Account Manager";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Show Intro Animation
            _ui.ShowStartupAnimation();

            // Auto-sync accounts on startup (background)
            _ = Task.Run(async () => await AutoSyncAccountsAsync());

            while (_running)
            {
                _ui.ShowHeader();
                _ui.ShowDashboard();

                var choice = _ui.ShowMainMenu();
                _ui.ShowMainMenuFooter();
                await HandleMainMenu(choice);
            }
        }

        private async Task HandleMainMenu(string choice)
        {
            switch (choice)
            {
                case "📋 View All Accounts":
                    await HandleViewAccounts();
                    break;
                case "➕ Add New Account":
                    await HandleAddAccount();
                    break;
                case "🚀 Quick Launch":
                    await HandleQuickLaunch();
                    break;
                case " Launch Queue":
                    await HandleLaunchQueue();
                    break;
                case "⚙️  Settings":
                    HandleSettings();
                    break;
                case "🚪 Exit":
                    _running = false;
                    AnsiConsole.MarkupLine("[grey]Goodbye! 👋[/]");
                    break;
            }
        }

        private async Task HandleViewAccounts()
        {
            while (true)
            {
                _ui.ShowHeader();
                var accounts = _storage.GetAllAccounts();
                _ui.ShowAccountsTable(accounts);

                var index = _ui.SelectAccount(accounts, "manage");
                if (index == null) break;

                var account = accounts[index.Value];
                await HandleAccountActions(account);
            }
        }

        private async Task HandleAccountActions(RiotAccount account)
        {
            while (true)
            {
                _ui.ShowHeader();
                _ui.ShowAccountDetails(account);
                AnsiConsole.WriteLine();

                var action = _ui.ShowExtendedAccountActions();

                switch (action)
                {
                    case "🚀 Launch Account":
                        await _launcher.LaunchAccount(account);
                        _ui.WaitForKey();
                        break;

                    case "✏️  Edit Account":
                        EditAccount(account);
                        break;

                    case "📊 View Details":
                        // Already showing details
                        _ui.WaitForKey();
                        break;

                    case "🕐 Mark as Played":
                        account.LastPlayed = DateTime.Now;
                        account.PlayHistory.Add(DateTime.Now);
                        account.TotalGamesPlayed++;
                        _storage.UpdateAccount(account);
                        AnsiConsole.MarkupLine("[green]✓ Marked as played just now![/]");
                        _ui.WaitForKey();
                        break;

                    case "� Sync with API":
                        await SyncAccountWithApi(account);
                        _ui.WaitForKey();
                        break;

                    case "�🗑️  Delete Account":
                        if (_ui.Confirm($"[red]Are you sure you want to delete {account.Username}?[/]"))
                        {
                            _storage.DeleteAccount(account.Id);
                            AnsiConsole.MarkupLine("[green]✓ Account deleted![/]");
                            _ui.WaitForKey();
                            return;
                        }
                        break;

                    case "🔙 Back":
                        return;
                }

                // Refresh account data
                account = _storage.GetAccount(account.Id) ?? account;
            }
        }

        private async Task HandleAddAccount()
        {
            _ui.ShowHeader();
            AnsiConsole.Write(new Rule("[yellow]➕ Add New Account[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            // Step 1: Get Login Username
            var username = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Login Username (email):[/]")
                    .PromptStyle("white")
                    .ValidationErrorMessage("[red]Cannot be empty[/]")
                    .Validate(u => !string.IsNullOrWhiteSpace(u)));

            // Step 2: Get Password
            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Password:[/]")
                    .PromptStyle("white")
                    .Secret()
                    .ValidationErrorMessage("[red]Cannot be empty[/]")
                    .Validate(p => !string.IsNullOrWhiteSpace(p)));

            // Step 3: Get and Validate Riot ID
            string riotId;
            AccountData? accountData = null;
            
            while (true)
            {
                riotId = AnsiConsole.Prompt(
                    new TextPrompt<string>("[cyan]Riot ID (Name#Tag):[/]")
                        .PromptStyle("white"));

                AnsiConsole.MarkupLine("[grey]Validating Riot ID...[/]");
                
                var (success, data, error) = await _apiService.ValidateAndFetchAccount(riotId);
                
                if (!success)
                {
                    AnsiConsole.MarkupLine($"[red]✗ {error}[/]");
                    if (!_ui.Confirm("[yellow]Try again?[/]"))
                    {
                        AnsiConsole.MarkupLine("[yellow]Account creation cancelled.[/]");
                        _ui.WaitForKey();
                        return;
                    }
                    continue;
                }

                accountData = data;
                
                if (!string.IsNullOrEmpty(error) && error.StartsWith("warn:"))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠ {error.Substring(5)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]✓ Found: {accountData!.Name}#{accountData.Tag} | Level {accountData.AccountLevel} | {accountData.Region}[/]");
                }
                break;
            }

            // Try to get rank
            string rank = "Unranked";
            var fetchedRank = await _apiService.FetchRank(accountData!.Name, accountData.Tag, accountData.Region);
            if (!string.IsNullOrEmpty(fetchedRank))
            {
                rank = fetchedRank;
                AnsiConsole.MarkupLine($"[green]✓ Rank: {rank}[/]");
            }

            // Create account
            var account = new RiotAccount
            {
                Username = username,
                EncryptedPassword = EncryptionService.Encrypt(password),
                RiotId = $"{accountData.Name}#{accountData.Tag}",
                DisplayName = accountData.Name,
                Region = accountData.Region,
                Level = accountData.AccountLevel,
                Rank = rank
            };

            AnsiConsole.WriteLine();
            if (_ui.Confirm("[green]Save this account?[/]"))
            {
                _storage.AddAccount(account);
                AnsiConsole.MarkupLine($"[green]✓ Account added successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            }

            _ui.WaitForKey();
        }

        private async Task HandleQuickLaunch()
        {
            _ui.ShowHeader();
            AnsiConsole.Write(new Rule("[yellow]🚀 Quick Launch[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var accounts = _storage.GetAllAccounts();
            
            // Show ready accounts first
            var readyAccounts = accounts.Where(a => a.IsReadyForDaily).ToList();
            
            if (readyAccounts.Any())
            {
                AnsiConsole.MarkupLine($"[green]{readyAccounts.Count} account(s) ready for daily mission![/]\n");
            }

            // Sort by ready status, then by time since last played
            var sortedAccounts = accounts
                .OrderByDescending(a => a.IsReadyForDaily)
                .ThenByDescending(a => a.LastPlayed == null)
                .ThenBy(a => a.LastPlayed)
                .ToList();

            _ui.ShowAccountsTable(sortedAccounts);

            var index = _ui.SelectAccount(sortedAccounts, "launch");
            if (index == null) return;

            var account = sortedAccounts[index.Value];

            if (!account.IsReadyForDaily)
            {
                if (!_ui.Confirm($"[yellow]This account has {account.HoursUntilReady:F1}h left on cooldown. Launch anyway?[/]"))
                {
                    return;
                }
            }

            await _launcher.LaunchAccount(account);
            _ui.WaitForKey();
        }

        private void HandleStatistics()
        {
            _ui.ShowHeader();
            var accounts = _storage.GetAllAccounts();
            _ui.ShowStatistics(accounts);
            _ui.WaitForKey();
        }

        private void HandleAdvancedStatistics()
        {
            _statsService.ShowAdvancedStatistics();
            _ui.WaitForKey();
        }

        private async Task HandleSearchFilter()
        {
            while (true)
            {
                _ui.ShowHeader();
                var choice = _ui.ShowSearchFilterMenu();
                List<RiotAccount>? filteredAccounts = null;

                switch (choice)
                {
                    case "🔎 Search by Name":
                        var query = _ui.PromptSearchQuery();
                        filteredAccounts = _storage.SearchAccounts(query);
                        AnsiConsole.MarkupLine($"\n[cyan]Found {filteredAccounts.Count} account(s)[/]\n");
                        break;

                    case "🌍 Filter by Region":
                        var region = _ui.SelectRegionFilter();
                        filteredAccounts = _storage.FilterByRegion(region);
                        AnsiConsole.MarkupLine($"\n[cyan]Found {filteredAccounts.Count} account(s) in {region}[/]\n");
                        break;

                    case "🏆 Filter by Rank":
                        var rank = _ui.SelectRankFilter();
                        filteredAccounts = _storage.FilterByRank(rank);
                        AnsiConsole.MarkupLine($"\n[cyan]Found {filteredAccounts.Count} {rank} account(s)[/]\n");
                        break;

                    case "📊 Filter by Status":
                        var status = _ui.SelectStatusFilter();
                        filteredAccounts = _storage.FilterByStatus(status);
                        AnsiConsole.MarkupLine($"\n[cyan]Found {filteredAccounts.Count} {status} account(s)[/]\n");
                        break;

                    case "✅ Show Ready Accounts":
                        filteredAccounts = _storage.GetReadyAccounts();
                        AnsiConsole.MarkupLine($"\n[cyan]Found {filteredAccounts.Count} ready account(s)[/]\n");
                        break;

                    case "🔙 Back":
                        return;
                }

                if (filteredAccounts != null)
                {
                    _ui.ShowAccountsTable(filteredAccounts);
                    
                    if (filteredAccounts.Any() && _ui.Confirm("\n[yellow]Do you want to manage one of these accounts?[/]"))
                    {
                        var index = _ui.SelectAccount(filteredAccounts, "manage");
                        if (index.HasValue)
                        {
                            await HandleAccountActions(filteredAccounts[index.Value]);
                        }
                    }
                }

                _ui.WaitForKey();
            }
        }

        private async Task HandleLaunchQueue()
        {
            while (true)
            {
                _ui.ShowHeader();
                var choice = _ui.ShowQueueMenu();

                switch (choice)
                {
                    case "🚀 Launch All Ready Accounts":
                        await _queueService.LaunchAllReadyAccountsAsync();
                        break;

                    case "🎯 Custom Selection":
                        var allAccounts = _storage.GetAllAccounts()
                            .Where(a => a.Status == AccountStatus.Active)
                            .ToList();
                        _ui.ShowAccountsTable(allAccounts);
                        var selectedAccounts = _ui.SelectMultipleAccounts(allAccounts);
                        
                        if (selectedAccounts.Any())
                        {
                            await _queueService.LaunchQueueAsync(selectedAccounts);
                        }
                        break;

                    case "🔙 Back":
                        return;
                }

                if (choice != "🔙 Back")
                {
                    _ui.WaitForKey();
                }
            }
        }

        private async Task SyncAccountWithApi(RiotAccount account)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]🔄 Syncing account data...[/]");

            if (string.IsNullOrEmpty(account.RiotId) || !account.RiotId.Contains('#'))
            {
                AnsiConsole.MarkupLine("[red]✗ Invalid Riot ID. Please edit it first.[/]");
                return;
            }

            var (success, data, error) = await _apiService.ValidateAndFetchAccount(account.RiotId);
            
            if (!success)
            {
                AnsiConsole.MarkupLine($"[red]✗ {error}[/]");
                return;
            }

            if (data != null)
            {
                account.DisplayName = data.Name;
                account.Region = data.Region;
                // Only update level if API returned a valid value (not 0 or 1)
                if (data.AccountLevel > 1 || account.Level == 0)
                {
                    account.Level = data.AccountLevel;
                }
                account.RiotId = $"{data.Name}#{data.Tag}";
                
                // Try to get rank
                var rank = await _apiService.FetchRank(data.Name, data.Tag, data.Region);
                if (!string.IsNullOrEmpty(rank))
                {
                    account.Rank = rank;
                }

                _storage.UpdateAccount(account);
                
                if (!string.IsNullOrEmpty(error) && error.StartsWith("warn:"))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠ {error.Substring(5)}[/]");
                }
                AnsiConsole.MarkupLine($"[green]✓ Synced! Level {account.Level} | {account.Region} | {account.Rank}[/]");
            }
        }

        /// <summary>
        /// Auto-sync all accounts in background
        /// </summary>
        private async Task AutoSyncAccountsAsync()
        {
            var accounts = _storage.GetAllAccounts();
            if (!accounts.Any()) return;

            foreach (var account in accounts)
            {
                if (string.IsNullOrEmpty(account.RiotId) || !account.RiotId.Contains('#'))
                    continue;

                try
                {
                    var (success, data, _) = await _apiService.ValidateAndFetchAccount(account.RiotId);
                    
                    if (success && data != null)
                    {
                        bool updated = false;
                        
                        // Update level only if API returned a valid value > 1
                        if (data.AccountLevel > 1 && data.AccountLevel != account.Level)
                        {
                            account.Level = data.AccountLevel;
                            updated = true;
                        }
                        
                        // Update region if different
                        if (!string.IsNullOrEmpty(data.Region) && data.Region != account.Region)
                        {
                            account.Region = data.Region;
                            updated = true;
                        }

                        // Try to get rank
                        var rank = await _apiService.FetchRank(data.Name, data.Tag, data.Region);
                        if (!string.IsNullOrEmpty(rank) && rank != account.Rank)
                        {
                            account.Rank = rank;
                            updated = true;
                        }

                        if (updated)
                        {
                            _storage.UpdateAccount(account);
                        }
                    }
                    
                    // Small delay between requests to avoid rate limiting
                    await Task.Delay(500);
                }
                catch
                {
                    // Silently ignore errors during background sync
                }
            }
        }

        /// <summary>
        /// Sync all accounts manually with progress display
        /// </summary>
        private async Task SyncAllAccountsAsync()
        {
            var accounts = _storage.GetAllAccounts();
            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No accounts to sync.[/]");
                return;
            }

            int synced = 0;
            int failed = 0;

            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[cyan]Syncing {accounts.Count} accounts...[/]", maxValue: accounts.Count);

                    foreach (var account in accounts)
                    {
                        task.Description = $"[cyan]Syncing {account.DisplayName ?? account.Username}...[/]";
                        
                        if (string.IsNullOrEmpty(account.RiotId) || !account.RiotId.Contains('#'))
                        {
                            failed++;
                            task.Increment(1);
                            continue;
                        }

                        try
                        {
                            var (success, data, _) = await _apiService.ValidateAndFetchAccount(account.RiotId);
                            
                            if (success && data != null)
                            {
                                if (data.AccountLevel > 1)
                                {
                                    account.Level = data.AccountLevel;
                                }
                                if (!string.IsNullOrEmpty(data.Region))
                                {
                                    account.Region = data.Region;
                                }

                                var rank = await _apiService.FetchRank(data.Name, data.Tag, data.Region);
                                if (!string.IsNullOrEmpty(rank))
                                {
                                    account.Rank = rank;
                                }

                                _storage.UpdateAccount(account);
                                synced++;
                            }
                            else
                            {
                                failed++;
                            }
                            
                            await Task.Delay(300);
                        }
                        catch
                        {
                            failed++;
                        }

                        task.Increment(1);
                    }
                });

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]✓ Synced {synced} account(s)[/]");
            if (failed > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠ Failed to sync {failed} account(s)[/]");
            }
        }

        private void HandleSettings()
        {
            while (true)
            {
                _ui.ShowHeader();
                var settings = _storage.GetSettings();
                _ui.ShowSettings(settings);

                var choice = _ui.ShowSettingsMenu();

                switch (choice)
                {
                    case "📁 Set Riot Client Path":
                        settings.RiotClientPath = _ui.PromptText("Enter Riot Client path:", settings.RiotClientPath);
                        _storage.UpdateSettings(settings);
                        AnsiConsole.MarkupLine("[green]✓ Path updated![/]");
                        break;

                    case "📁 Set Valorant Path":
                        settings.ValorantPath = _ui.PromptText("Enter Valorant path:", settings.ValorantPath);
                        _storage.UpdateSettings(settings);
                        AnsiConsole.MarkupLine("[green]✓ Path updated![/]");
                        break;

                    case "⏱️  Set Login Delay":
                        settings.LoginDelayMs = _ui.PromptInt("Enter login delay (ms):", settings.LoginDelayMs);
                        _storage.UpdateSettings(settings);
                        AnsiConsole.MarkupLine("[green]✓ Delay updated![/]");
                        break;

                    case "🌍 Set Default Region":
                        settings.DefaultRegion = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[cyan]Select default region:[/]")
                                .AddChoices(new[] { "EU", "NA", "AP", "KR", "LATAM", "BR" }));
                        _storage.UpdateSettings(settings);
                        AnsiConsole.MarkupLine("[green]✓ Region updated![/]");
                        break;

                    case "🔑 Set API Key":
                        AnsiConsole.MarkupLine("[grey]Get API key from: https://api.henrikdev.xyz/dashboard/[/]");
                        var apiKey = AnsiConsole.Prompt(
                            new TextPrompt<string>("[cyan]Enter API Key (or empty to remove):[/]")
                                .AllowEmpty());
                        settings.HenrikApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
                        _storage.UpdateSettings(settings);
                        _apiService.SetApiKey(settings.HenrikApiKey);
                        AnsiConsole.MarkupLine(string.IsNullOrEmpty(settings.HenrikApiKey) 
                            ? "[yellow]API key removed[/]" 
                            : "[green]✓ API key saved![/]");
                        break;

                    case "🔄 Sync All Accounts":
                        Task.Run(async () => await SyncAllAccountsAsync()).Wait();
                        break;

                    case "🔄 Toggle Auto Close":
                        settings.AutoCloseClient = !settings.AutoCloseClient;
                        _storage.UpdateSettings(settings);
                        AnsiConsole.MarkupLine($"[green]✓ Auto close is now {(settings.AutoCloseClient ? "enabled" : "disabled")}![/]");
                        break;

                    case "🔙 Back":
                        return;
                }

                _ui.WaitForKey();
            }
        }

        private void EditAccount(RiotAccount account)
        {
            _ui.ShowHeader();
            AnsiConsole.Write(new Rule($"[yellow]Edit {account.Username}[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var editChoices = new List<string>
            {
                "📝 Edit Display Name",
                "🆔 Edit Riot ID",
                "🔑 Change Password",
                "📈 Update Level",
                "💎 Update AP",
                "🏆 Update Rank",
                "🌍 Change Region",
                "📌 Update Status",
                "📝 Edit Notes",
                "🔙 Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to edit?[/]")
                    .AddChoices(editChoices));

            switch (choice)
            {
                case "📝 Edit Display Name":
                    account.DisplayName = _ui.PromptText("New display name:", account.DisplayName ?? "");
                    break;

                case "🆔 Edit Riot ID":
                    var newRiotId = AnsiConsole.Prompt(
                        new TextPrompt<string>("[cyan]New Riot ID (name#tag):[/]")
                            .DefaultValue(account.RiotId)
                            .ValidationErrorMessage("[red]Invalid format! Use: name#tag[/]")
                            .Validate(id => 
                            {
                                if (string.IsNullOrWhiteSpace(id)) return ValidationResult.Error("Riot ID cannot be empty");
                                if (!id.Contains('#')) return ValidationResult.Error("Format must be: name#tag");
                                var parts = id.Split('#');
                                if (parts.Length != 2) return ValidationResult.Error("Format must be: name#tag");
                                if (string.IsNullOrWhiteSpace(parts[0])) return ValidationResult.Error("Name cannot be empty");
                                if (string.IsNullOrWhiteSpace(parts[1])) return ValidationResult.Error("Tag cannot be empty");
                                return ValidationResult.Success();
                            }));
                    account.RiotId = newRiotId;
                    break;

                case "🔑 Change Password":
                    var newPassword = AnsiConsole.Prompt(
                        new TextPrompt<string>("[cyan]New password:[/]")
                            .Secret());
                    account.EncryptedPassword = EncryptionService.Encrypt(newPassword);
                    break;

                case "📈 Update Level":
                    account.Level = _ui.PromptInt("New level:", account.Level);
                    break;

                case "💎 Update AP":
                    account.AP = _ui.PromptInt("New AP:", account.AP);
                    break;

                case "🏆 Update Rank":
                    account.Rank = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Select rank:[/]")
                            .AddChoices(new[] { 
                                "Unranked", "Iron", "Bronze", "Silver", "Gold", 
                                "Platinum", "Diamond", "Ascendant", "Immortal", "Radiant" 
                            }));
                    break;

                case "🌍 Change Region":
                    account.Region = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Select region:[/]")
                            .AddChoices(new[] { "EU", "NA", "AP", "KR", "LATAM", "BR" }));
                    break;

                case "📌 Update Status":
                    var statusChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[cyan]Select status:[/]")
                            .AddChoices(new[] { "Active", "Banned", "Suspended", "Inactive" }));
                    account.Status = Enum.Parse<AccountStatus>(statusChoice);
                    break;

                case "📝 Edit Notes":
                    account.Notes = _ui.PromptText("Notes:", account.Notes ?? "");
                    break;

                case "🔙 Back":
                    return;
            }

            _storage.UpdateAccount(account);
            AnsiConsole.MarkupLine("[green]✓ Account updated![/]");
            _ui.WaitForKey();
        }
    }
}
