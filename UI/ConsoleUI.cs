using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spectre.Console;
using VAM.Models;
using VAM.Services;

namespace VAM.UI
{
    public class ConsoleUI
    {
        private readonly AccountStorage _storage;
        private readonly RiotLauncher _launcher;

        public ConsoleUI()
        {
            _storage = new AccountStorage();
            _launcher = new RiotLauncher(_storage);
        }

        public void ShowStartupAnimation()
        {
            Console.Clear();
            AnsiConsole.Cursor.Hide();

            // Intro Logo Animation
            var logoLines = new[]
            {
                "[red bold]╦  ╦[/][white bold]───[/][red bold]╔═╗[/][white bold]╔╦╗[/]",
                "[red bold]║  ║[/][white bold]───[/][red bold]╠═╣[/][white bold] ║║[/]",
                "[red bold]╚═╝╚[/][white bold]═══[/][red bold]╩ ╩[/][white bold]═╩╝[/]",
                "[grey dim]━━━━━━━━━━━━━━━━━━[/]",
                "[red]V[/][white]alorant [/][red]A[/][white]ccount [/][red]M[/][white]anager[/]"
            };

            var panel = new Panel(Align.Center(new Markup(string.Join("\n", logoLines)), VerticalAlignment.Middle))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(new Color(255, 70, 85)),
                Padding = new Padding(4, 2, 4, 2)
            };

            // Animate Panel Border
            AnsiConsole.Write(new Padder(panel).Padding(0, 2));
            
            // Simulate loading Tasks
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("red"))
                .Start("Initializing V-AM...", ctx => 
                {
                    Thread.Sleep(600);
                    ctx.Status("Loading Accounts Database...");
                    Thread.Sleep(500);
                    ctx.Status("Checking Riot Services...");
                    Thread.Sleep(500);
                    ctx.Status("Syncing Local Data...");
                    Thread.Sleep(400);
                });
                
            AnsiConsole.Cursor.Show();
            Console.Clear();
        }

        public void ShowHeader()
        {
            Console.Clear();
            
            // Valorant-themed ASCII Logo
            var logo = new Panel(
                Align.Center(
                    new Markup(
                        "[red bold]╦  ╦[/][white bold]───[/][red bold]╔═╗[/][white bold]╔╦╗[/]\n" +
                        "[red bold]║  ║[/][white bold]───[/][red bold]╠═╣[/][white bold] ║║[/]\n" +
                        "[red bold]╚═╝╚[/][white bold]═══[/][red bold]╩ ╩[/][white bold]═╩╝[/]\n" +
                        "[grey dim]━━━━━━━━━━━━━━━━━━[/]\n" +
                        "[red]V[/][white]alorant [/][red]A[/][white]ccount [/][red]M[/][white]anager[/]"
                    ),
                    VerticalAlignment.Middle
                ))
            {
                Border = BoxBorder.None,
                Padding = new Padding(0, 0, 0, 0)
            };
            
            AnsiConsole.Write(logo);
            AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
            AnsiConsole.WriteLine();
        }

        public void ShowFooter()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
            AnsiConsole.Markup("[grey dim]Powered By: [/][red]CursedTools[/][grey dim], [/][white]defextra[/]");
            AnsiConsole.WriteLine();
        }

        public void ShowDashboard()
        {
            var accounts = _storage.GetAllAccounts();
            var readyAccounts = accounts.Count(a => a.IsReadyForDaily);
            var totalAccounts = accounts.Count;
            var avgLevel = accounts.Any() ? (int)accounts.Average(a => a.Level) : 0;

            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow(
                CreateStatPanel("📊 Total", totalAccounts.ToString(), "#BD3944", "Accounts"),
                CreateStatPanel("✅ Ready", readyAccounts.ToString(), "#0ACF83", "For Daily"),
                CreateStatPanel("⏳ Cooldown", (totalAccounts - readyAccounts).ToString(), "#FFC700", "Waiting"),
                CreateStatPanel("📈 Avg Level", avgLevel.ToString(), "#FF4655", "All Accounts")
            );

            var dashboardPanel = new Panel(grid)
            {
                Header = new PanelHeader("[red bold]╔═══[/][white bold] DASHBOARD [/][red bold]═══╗[/]", Justify.Center),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(new Color(255, 70, 85)),
                Padding = new Padding(1, 1, 1, 1)
            };

            AnsiConsole.Write(dashboardPanel);
            AnsiConsole.WriteLine();
        }

        private Panel CreateStatPanel(string title, string value, string hexColor, string? subtitle = null)
        {
            var content = subtitle != null 
                ? new Markup($"[{hexColor} dim]{title}[/]\n[bold {hexColor}]{value}[/]\n[grey dim]{subtitle}[/]")
                : new Markup($"[{hexColor} dim]{title}[/]\n[bold {hexColor}]{value}[/]");
            
            // Parse hex color for border (convert hex to RGB)
            var hex = hexColor.Replace("#", "");
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            var borderColor = new Color(r, g, b);
            
            return new Panel(Align.Center(content, VerticalAlignment.Middle))
            {
                Border = BoxBorder.Heavy,
                BorderStyle = new Style(borderColor),
                Padding = new Padding(2, 1, 2, 1)
            };
        }

        public string ShowMainMenu()
        {
            AnsiConsole.WriteLine();
            
            var choices = new List<string>
            {
                "📋 View All Accounts",
                "➕ Add New Account",
                "🚀 Quick Launch",
                " Launch Queue",
                "⚙️  Settings",
                "🚪 Exit"
            };

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[red bold]╔═══════[/][white bold] MAIN MENU [/][red bold]═══════╗[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to see more)[/]")
                    .HighlightStyle(new Style(new Color(255, 70, 85), decoration: Decoration.Bold))
                    .AddChoices(choices));
        }

        public void ShowMainMenuFooter()
        {
            ShowFooter();
        }

        public void ShowAccountsTable(List<RiotAccount> accounts)
        {
            if (!accounts.Any())
            {
                var emptyPanel = new Panel(
                    Align.Center(
                        new Markup("[yellow]📭 No accounts found[/]\n[grey dim]Add your first account to get started![/]"),
                        VerticalAlignment.Middle
                    ))
                {
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow),
                    Padding = new Padding(2, 1, 2, 1)
                };
                AnsiConsole.Write(emptyPanel);
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(new Color(80, 80, 80)) // Darker border for minimalism
                .Title("[white bold]YOUR ACCOUNTS[/]")
                .Expand();

            // Minimal headers
            table.AddColumn(new TableColumn("[grey]#[/]").Centered().Width(4));
            table.AddColumn(new TableColumn("[white bold]ID[/]").Width(20));
            table.AddColumn(new TableColumn("[grey]Tag[/]").Width(15));
            table.AddColumn(new TableColumn("[white]Lvl[/]").Centered().Width(6));
            table.AddColumn(new TableColumn("[white]Rank[/]").Centered().Width(15));
            table.AddColumn(new TableColumn("[grey]Last[/]").Centered().Width(12));
            table.AddColumn(new TableColumn("[grey]State[/]").Centered().Width(8));
            table.AddColumn(new TableColumn("[grey]Daily[/]").Centered().Width(8));

            int index = 1;
            foreach (var account in accounts)
            {
                var statusColor = account.Status switch
                {
                    AccountStatus.Active => "green",
                    AccountStatus.Banned => "red",
                    AccountStatus.Suspended => "yellow",
                    _ => "grey"
                };

                var statusIcon = account.Status switch
                {
                    AccountStatus.Active => "✔",
                    AccountStatus.Banned => "🔒",
                    AccountStatus.Suspended => "⚠",
                    _ => "○"
                };

                var readyIcon = account.IsReadyForDaily 
                    ? "[green bold]READY[/]" 
                    : $"[grey]{account.HoursUntilReady:F1}h[/]";
                
                var levelColor = account.Level switch
                {
                    >= 100 => "#FF4655 bold",
                    >= 50 => "#BD3944",
                    >= 20 => "#FFC700",
                    _ => "grey"
                };

                var rankColor = GetRankColor(account.Rank);

                table.AddRow(
                    $"[grey dim]{index}[/]",
                    $"[white bold]{account.Username}[/]",
                    $"[grey]{account.DisplayName ?? "-"}[/]",
                    $"[{levelColor}]{account.Level}[/]",
                    $"[{rankColor}]{account.Rank ?? "Unranked"}[/]",
                    $"[grey dim]{account.TimeSinceLastPlayed}[/]",
                    $"[{statusColor}]{statusIcon}[/]",
                    readyIcon
                );
                index++;
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private string GetRankColor(string? rank)
        {
            if (string.IsNullOrEmpty(rank)) return "grey";
            var r = rank.ToLower();
            if (r.Contains("iron")) return "#585858";
            if (r.Contains("bronze")) return "#A2783F";
            if (r.Contains("silver")) return "#C0C0C0"; 
            if (r.Contains("gold")) return "#FFD700"; 
            if (r.Contains("platinum")) return "#3CA6A6"; 
            if (r.Contains("diamond")) return "#B946E6"; 
            if (r.Contains("ascendant")) return "#44CF86"; 
            if (r.Contains("immortal")) return "#FF4655"; 
            if (r.Contains("radiant")) return "#FFFFBE bold"; 
            return "blue";
        }

        public RiotAccount? ShowAddAccountForm()
        {
            AnsiConsole.Write(new Rule("[yellow]Add New Account[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var username = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Username:[/]")
                    .PromptStyle("white")
                    .ValidationErrorMessage("[red]Username cannot be empty[/]")
                    .Validate(name => !string.IsNullOrWhiteSpace(name)));

            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Password:[/]")
                    .PromptStyle("white")
                    .Secret()
                    .ValidationErrorMessage("[red]Password cannot be empty[/]")
                    .Validate(pass => !string.IsNullOrWhiteSpace(pass)));

            var riotId = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Riot ID (e.g., PlayerName#EUW1):[/]")
                    .PromptStyle("white")
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

            var displayName = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Display Name (optional):[/]")
                    .PromptStyle("white")
                    .AllowEmpty());

            var region = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Region:[/]")
                    .AddChoices(new[] { "EU", "NA", "AP", "KR", "LATAM", "BR" }));

            var level = AnsiConsole.Prompt(
                new TextPrompt<int>("[cyan]Current Level:[/]")
                    .PromptStyle("white")
                    .DefaultValue(1)
                    .ValidationErrorMessage("[red]Please enter a valid level[/]"));

            var ap = AnsiConsole.Prompt(
                new TextPrompt<int>("[cyan]Current AP:[/]")
                    .PromptStyle("white")
                    .DefaultValue(0));

            var rank = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Rank:[/]")
                    .AddChoices(new[] { 
                        "Unranked", 
                        "Iron", "Bronze", "Silver", "Gold", 
                        "Platinum", "Diamond", "Ascendant", 
                        "Immortal", "Radiant" 
                    }));

            var notes = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Notes (optional):[/]")
                    .PromptStyle("white")
                    .AllowEmpty());

            var account = new RiotAccount
            {
                Username = username,
                EncryptedPassword = EncryptionService.Encrypt(password),
                RiotId = riotId,
                DisplayName = string.IsNullOrEmpty(displayName) ? null : displayName,
                Region = region,
                Level = level,
                AP = ap,
                Rank = rank,
                Notes = string.IsNullOrEmpty(notes) ? null : notes
            };

            if (AnsiConsole.Confirm("[yellow]Save this account?[/]"))
            {
                return account;
            }

            return null;
        }

        public int? SelectAccount(List<RiotAccount> accounts, string action)
        {
            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No accounts available.[/]");
                return null;
            }

            var choices = accounts.Select((a, i) => 
                $"{i + 1}. {a.Username} {(a.IsReadyForDaily ? "[green](Ready)[/]" : $"[yellow]({a.HoursUntilReady:F1}h)[/]")}"
            ).ToList();
            choices.Add("🔙 Back");

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[yellow]Select account to {action}:[/]")
                    .PageSize(15)
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .AddChoices(choices));

            if (selection == "🔙 Back") return null;

            var index = choices.IndexOf(selection);
            return index >= 0 && index < accounts.Count ? index : null;
        }

        public string ShowAccountActions()
        {
            var choices = new List<string>
            {
                "🚀 Launch Account",
                "✏️  Edit Account",
                "📊 View Details",
                "🕐 Mark as Played",
                "🗑️  Delete Account",
                "🔙 Back"
            };

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Account Actions[/]")
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .AddChoices(choices));
        }

        public void ShowAccountDetails(RiotAccount account)
        {
            var levelColor = account.Level switch
            {
                >= 100 => "magenta bold",
                >= 50 => "cyan1",
                >= 20 => "green",
                _ => "white"
            };

            var statusIcon = account.Status switch
            {
                AccountStatus.Active => "✓",
                AccountStatus.Banned => "✗",
                AccountStatus.Suspended => "⚠",
                _ => "○"
            };

            var readyStatus = account.IsReadyForDaily 
                ? "[green bold]✓ READY FOR DAILY[/]" 
                : $"[yellow]⏳ Cooldown: {account.HoursUntilReady:F1}h remaining[/]";

            var grid = new Grid()
                .AddColumn(new GridColumn().Width(18))
                .AddColumn()
                .AddRow("[cyan1 bold]🆔 Username:[/]", $"[white bold]{account.Username}[/]")
                .AddRow("[cyan1 bold]🎮 Riot ID:[/]", $"[white]{account.RiotId}[/]")
                .AddRow("[cyan1 bold]📝 Display:[/]", $"[grey]{account.DisplayName ?? "-"}[/]")
                .AddEmptyRow()
                .AddRow("[magenta bold]📊 Level:[/]", $"[{levelColor}]{account.Level}[/]")
                .AddRow("[blue bold]🏆 Rank:[/]", $"[blue]{account.Rank ?? "Unranked"}[/]")
                .AddRow("[yellow bold]💎 AP:[/]", $"[yellow]{account.AP}[/]")
                .AddRow("[cyan1 bold]🌍 Region:[/]", $"[white]{account.Region}[/]")
                .AddEmptyRow()
                .AddRow("[green bold]📅 Status:[/]", $"[{GetStatusColor(account.Status)}]{statusIcon} {account.Status}[/]")
                .AddRow("[grey bold]🕐 Last Played:[/]", $"[grey]{account.LastPlayed?.ToString("g") ?? "Never"}[/]")
                .AddRow("[grey bold]⏱️  Time Since:[/]", $"[grey dim]{account.TimeSinceLastPlayed}[/]")
                .AddRow("[green bold]⚡ Daily Status:[/]", readyStatus)
                .AddEmptyRow()
                .AddRow("[grey bold]📅 Created:[/]", $"[grey dim]{account.CreatedAt:g}[/]")
                .AddRow("[grey bold]📝 Notes:[/]", $"[grey dim]{account.Notes ?? "-"}[/]");

            var panel = new Panel(grid)
            {
                Header = new PanelHeader($"[red bold]╔═══════[/][white bold] {account.DisplayName ?? account.Username} [/][red bold]═══════╗[/]", Justify.Center),
                Border = BoxBorder.Heavy,
                BorderStyle = new Style(new Color(255, 70, 85)),
                Padding = new Padding(3, 1, 3, 1)
            };

            AnsiConsole.Write(panel);
        }

        private string GetStatusColor(AccountStatus status) => status switch
        {
            AccountStatus.Active => "green",
            AccountStatus.Banned => "red",
            AccountStatus.Suspended => "yellow",
            _ => "grey"
        };

        public void ShowStatistics(List<RiotAccount> accounts)
        {
            AnsiConsole.Write(new Rule("[yellow]📊 Statistics[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No accounts to show statistics for.[/]");
                return;
            }

            // Summary Panel
            var avgLevel = accounts.Average(a => a.Level);
            var totalAP = accounts.Sum(a => a.AP);
            var readyCount = accounts.Count(a => a.IsReadyForDaily);

            var summaryGrid = new Grid();
            summaryGrid.AddColumn();
            summaryGrid.AddColumn();
            summaryGrid.AddColumn();

            summaryGrid.AddRow(
                CreateStatPanel("📈 Avg Level", avgLevel.ToString("F1"), "cyan"),
                CreateStatPanel("💎 Total AP", totalAP.ToString("N0"), "magenta"),
                CreateStatPanel("✅ Ready Now", $"{readyCount}/{accounts.Count}", "green")
            );

            AnsiConsole.Write(summaryGrid);
            AnsiConsole.WriteLine();

            // Region Distribution
            var regionChart = new BarChart()
                .Width(60)
                .Label("[cyan]Accounts by Region[/]");

            var regionGroups = accounts.GroupBy(a => a.Region ?? "Unknown")
                .OrderByDescending(g => g.Count());

            foreach (var group in regionGroups)
            {
                regionChart.AddItem(group.Key, group.Count(), Color.Red);
            }

            AnsiConsole.Write(regionChart);
            AnsiConsole.WriteLine();

            // Level Distribution
            var levelChart = new BarChart()
                .Width(60)
                .Label("[cyan]Accounts by Level Range[/]");

            var levelRanges = new Dictionary<string, int>
            {
                { "1-20", accounts.Count(a => a.Level >= 1 && a.Level <= 20) },
                { "21-40", accounts.Count(a => a.Level >= 21 && a.Level <= 40) },
                { "41-60", accounts.Count(a => a.Level >= 41 && a.Level <= 60) },
                { "61-80", accounts.Count(a => a.Level >= 61 && a.Level <= 80) },
                { "81-100", accounts.Count(a => a.Level >= 81 && a.Level <= 100) },
                { "100+", accounts.Count(a => a.Level > 100) }
            };

            foreach (var range in levelRanges.Where(r => r.Value > 0))
            {
                levelChart.AddItem(range.Key, range.Value, Color.Cyan1);
            }

            AnsiConsole.Write(levelChart);
            AnsiConsole.WriteLine();

            // Ready accounts list
            if (readyCount > 0)
            {
                AnsiConsole.Write(new Rule("[green]Ready for Daily Mission[/]").RuleStyle("grey"));
                var readyAccounts = accounts.Where(a => a.IsReadyForDaily).ToList();
                foreach (var acc in readyAccounts)
                {
                    AnsiConsole.MarkupLine($"  [green]✓[/] {acc.Username} [grey]({acc.TimeSinceLastPlayed})[/]");
                }
            }
        }

        public void ShowSettings(AppSettings settings)
        {
            AnsiConsole.Write(new Rule("[yellow]⚙️ Settings[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var apiKeyStatus = string.IsNullOrEmpty(settings.HenrikApiKey) ? "[red]Not Set[/]" : "[green]Set ✓[/]";

            var panel = new Panel(new Grid()
                .AddColumn()
                .AddColumn()
                .AddRow("[cyan]Riot Client Path:[/]", $"[white]{settings.RiotClientPath}[/]")
                .AddRow("[cyan]Valorant Path:[/]", $"[white]{settings.ValorantPath}[/]")
                .AddRow("[cyan]Login Delay:[/]", $"[white]{settings.LoginDelayMs}ms[/]")
                .AddRow("[cyan]Default Region:[/]", $"[white]{settings.DefaultRegion}[/]")
                .AddRow("[cyan]Auto Close Client:[/]", settings.AutoCloseClient ? "[green]Yes[/]" : "[red]No[/]")
                .AddRow("[cyan]Henrik API Key:[/]", apiKeyStatus))
            {
                Header = new PanelHeader("[bold]Current Settings[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        public string ShowSettingsMenu()
        {
            var choices = new List<string>
            {
                "📁 Set Riot Client Path",
                "📁 Set Valorant Path",
                "⏱️  Set Login Delay",
                "🌍 Set Default Region",
                "🔑 Set API Key",
                "🔄 Sync All Accounts",
                "🔄 Toggle Auto Close",
                "🔙 Back"
            };

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Settings Menu[/]")
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .AddChoices(choices));
        }

        public void WaitForKey(string message = "Press any key to continue...")
        {
            AnsiConsole.MarkupLine($"\n[grey]{message}[/]");
            Console.ReadKey(true);
        }

        public bool Confirm(string message)
        {
            return AnsiConsole.Confirm(message);
        }

        public string PromptText(string prompt, string? defaultValue = null)
        {
            var textPrompt = new TextPrompt<string>($"[cyan]{prompt}[/]")
                .PromptStyle("white");

            if (!string.IsNullOrEmpty(defaultValue))
            {
                textPrompt.DefaultValue(defaultValue);
            }

            return AnsiConsole.Prompt(textPrompt);
        }

        public int PromptInt(string prompt, int defaultValue = 0)
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>($"[cyan]{prompt}[/]")
                    .PromptStyle("white")
                    .DefaultValue(defaultValue));
        }

        // Search and Filter UI Methods
        public string ShowSearchFilterMenu()
        {
            var choices = new List<string>
            {
                "🔎 Search by Name",
                "🌍 Filter by Region",
                "🏆 Filter by Rank",
                "📊 Filter by Status",
                "✅ Show Ready Accounts",
                "🔙 Back"
            };

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]🔍 Search & Filter[/]")
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .AddChoices(choices));
        }

        public string PromptSearchQuery()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Enter search query:[/]")
                    .PromptStyle("white")
                    .ValidationErrorMessage("[red]Search query cannot be empty[/]")
                    .Validate(q => !string.IsNullOrWhiteSpace(q)));
        }

        public string SelectRegionFilter()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select region:[/]")
                    .AddChoices(new[] { "EU", "NA", "AP", "KR", "LATAM", "BR" }));
        }

        public string SelectRankFilter()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select rank:[/]")
                    .AddChoices(new[] { 
                        "Unranked", "Iron", "Bronze", "Silver", "Gold", 
                        "Platinum", "Diamond", "Ascendant", "Immortal", "Radiant" 
                    }));
        }

        public AccountStatus SelectStatusFilter()
        {
            var status = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select status:[/]")
                    .AddChoices(new[] { "Active", "Banned", "Suspended", "Inactive" }));
            return Enum.Parse<AccountStatus>(status);
        }

        public string PromptGroupName()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Enter group name:[/]")
                    .PromptStyle("white")
                    .ValidationErrorMessage("[red]Group name cannot be empty[/]")
                    .Validate(g => !string.IsNullOrWhiteSpace(g)));
        }

        // Queue UI Methods
        public string ShowQueueMenu()
        {
            var choices = new List<string>
            {
                "🚀 Launch All Ready Accounts",
                "🎯 Custom Selection",
                "🔙 Back"
            };

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]🚦 Launch Queue[/]")
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .AddChoices(choices));
        }

        public List<RiotAccount> SelectMultipleAccounts(List<RiotAccount> accounts)
        {
            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No accounts available.[/]");
                return new List<RiotAccount>();
            }

            var choices = accounts.Select(a => 
                $"{a.Username} {(a.IsReadyForDaily ? "[green](Ready)[/]" : $"[yellow]({a.HoursUntilReady:F1}h)[/]")}"
            ).ToList();

            var selected = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[yellow]Select accounts to launch (Space to select, Enter to confirm):[/]")
                    .PageSize(15)
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                    .AddChoices(choices));

            return accounts.Where(a => 
                selected.Any(s => s.Contains(a.Username))
            ).ToList();
        }

        public int PromptQueueDelay()
        {
            return AnsiConsole.Prompt(
                new TextPrompt<int>("[cyan]Delay between accounts (minutes):[/]")
                    .PromptStyle("white")
                    .DefaultValue(5)
                    .ValidationErrorMessage("[red]Please enter a valid number[/]")
                    .Validate(d => d >= 1 && d <= 60));
        }

        // Account Actions Extended
        public string ShowExtendedAccountActions()
        {
            var choices = new List<string>
            {
                "🚀 Launch Account",
                "✏️  Edit Account",
                "📊 View Details",
                "🕐 Mark as Played",
                "🔁 Sync with API",
                "🗑️ Delete Account",
                "🔙 Back"
            };

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Account Actions[/]")
                    .HighlightStyle(Style.Parse("cyan bold"))
                    .AddChoices(choices));
        }
    }
}
