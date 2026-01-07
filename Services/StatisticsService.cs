using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using VAM.Models;

namespace VAM.Services
{
    /// <summary>
    /// Advanced statistics service for account analytics
    /// </summary>
    public class StatisticsService
    {
        private readonly AccountStorage _storage;

        public StatisticsService(AccountStorage storage)
        {
            _storage = storage;
        }

        public void ShowAdvancedStatistics()
        {
            var accounts = _storage.GetAllAccounts();
            
            if (!accounts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No accounts to analyze[/]");
                return;
            }

            Console.Clear();
            AnsiConsole.Write(new FigletText("Statistics").Color(Color.Cyan1).Centered());
            AnsiConsole.Write(new Rule("[grey]Advanced Analytics[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            // Overview Grid
            ShowOverviewPanel(accounts);
            AnsiConsole.WriteLine();

            // Activity Analysis
            ShowActivityAnalysis(accounts);
            AnsiConsole.WriteLine();

            // Distribution Charts
            ShowDistributionCharts(accounts);
            AnsiConsole.WriteLine();

            // Top Accounts
            ShowTopAccounts(accounts);
            AnsiConsole.WriteLine();

            // Groups Summary
            ShowGroupsSummary(accounts);
        }

        private void ShowOverviewPanel(List<RiotAccount> accounts)
        {
            var totalAccounts = accounts.Count;
            var readyAccounts = accounts.Count(a => a.IsReadyForDaily);
            var favoriteAccounts = accounts.Count(a => a.IsFavorite);
            var avgLevel = accounts.Average(a => a.Level);
            var totalAP = accounts.Sum(a => a.AP);
            var maxLevel = accounts.Max(a => a.Level);
            var bannedAccounts = accounts.Count(a => a.Status == AccountStatus.Banned);

            var grid = new Grid().Expand();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();
            grid.AddColumn();

            grid.AddRow(
                new Panel($"[bold cyan]{totalAccounts}[/]\n[grey]Total Accounts[/]").RoundedBorder(),
                new Panel($"[bold green]{readyAccounts}[/]\n[grey]Ready Now[/]").RoundedBorder(),
                new Panel($"[bold yellow]{favoriteAccounts}[/]\n[grey]Favorites[/]").RoundedBorder(),
                new Panel($"[bold red]{bannedAccounts}[/]\n[grey]Banned[/]").RoundedBorder()
            );

            grid.AddRow(
                new Panel($"[bold magenta]{avgLevel:F1}[/]\n[grey]Avg Level[/]").RoundedBorder(),
                new Panel($"[bold blue]{maxLevel}[/]\n[grey]Max Level[/]").RoundedBorder(),
                new Panel($"[bold green]{totalAP:N0}[/]\n[grey]Total AP[/]").RoundedBorder(),
                new Panel($"[bold cyan]{accounts.Sum(a => a.TotalGamesPlayed)}[/]\n[grey]Total Games[/]").RoundedBorder()
            );

            AnsiConsole.Write(grid);
        }

        private void ShowActivityAnalysis(List<RiotAccount> accounts)
        {
            AnsiConsole.Write(new Rule("[cyan]📊 Activity Analysis[/]").LeftJustified());
            
            var playedLast24h = accounts.Count(a => a.LastPlayed.HasValue && (DateTime.Now - a.LastPlayed.Value).TotalHours < 24);
            var playedLast7d = accounts.Count(a => a.LastPlayed.HasValue && (DateTime.Now - a.LastPlayed.Value).TotalDays < 7);
            var neverPlayed = accounts.Count(a => !a.LastPlayed.HasValue);
            var inactive30d = accounts.Count(a => a.LastPlayed.HasValue && (DateTime.Now - a.LastPlayed.Value).TotalDays > 30);

            var activityTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
            activityTable.AddColumn(new TableColumn("[cyan]Period[/]").Centered());
            activityTable.AddColumn(new TableColumn("[cyan]Accounts[/]").Centered());
            activityTable.AddColumn(new TableColumn("[cyan]Percentage[/]").Centered());

            activityTable.AddRow("Last 24 hours", playedLast24h.ToString(), $"{(playedLast24h * 100.0 / accounts.Count):F1}%");
            activityTable.AddRow("Last 7 days", playedLast7d.ToString(), $"{(playedLast7d * 100.0 / accounts.Count):F1}%");
            activityTable.AddRow("Never played", neverPlayed.ToString(), $"{(neverPlayed * 100.0 / accounts.Count):F1}%");
            activityTable.AddRow("Inactive 30+ days", inactive30d.ToString(), $"{(inactive30d * 100.0 / accounts.Count):F1}%");

            AnsiConsole.Write(activityTable);
        }

        private void ShowDistributionCharts(List<RiotAccount> accounts)
        {
            AnsiConsole.Write(new Rule("[cyan]📈 Distribution Charts[/]").LeftJustified());

            // Rank Distribution
            var rankGroups = accounts
                .GroupBy(a => a.Rank ?? "Unranked")
                .OrderByDescending(g => g.Count())
                .Take(10);

            var rankChart = new BarChart()
                .Width(60)
                .Label("[cyan bold]Rank Distribution[/]");

            foreach (var group in rankGroups)
            {
                rankChart.AddItem(group.Key, group.Count(), GetRankColor(group.Key));
            }

            AnsiConsole.Write(rankChart);
            AnsiConsole.WriteLine();

            // Region Distribution
            var regionGroups = accounts
                .GroupBy(a => a.Region ?? "Unknown")
                .OrderByDescending(g => g.Count());

            var regionChart = new BarChart()
                .Width(60)
                .Label("[cyan bold]Region Distribution[/]");

            foreach (var group in regionGroups)
            {
                regionChart.AddItem(group.Key, group.Count(), Color.Blue);
            }

            AnsiConsole.Write(regionChart);
        }

        private void ShowTopAccounts(List<RiotAccount> accounts)
        {
            AnsiConsole.Write(new Rule("[cyan]🏆 Top Accounts[/]").LeftJustified());

            var topByLevel = accounts.OrderByDescending(a => a.Level).Take(5);
            var topByAP = accounts.OrderByDescending(a => a.AP).Take(5);

            var grid = new Grid().AddColumn().AddColumn();

            // Top by Level
            var levelTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Width(50);
            levelTable.AddColumn("[cyan]Username[/]");
            levelTable.AddColumn("[cyan]Level[/]").Centered();

            foreach (var acc in topByLevel)
            {
                levelTable.AddRow(acc.Username, $"[green]{acc.Level}[/]");
            }

            // Top by AP
            var apTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Width(50);
            apTable.AddColumn("[cyan]Username[/]");
            apTable.AddColumn("[cyan]AP[/]").Centered();

            foreach (var acc in topByAP)
            {
                apTable.AddRow(acc.Username, $"[magenta]{acc.AP:N0}[/]");
            }

            grid.AddRow(
                new Panel(levelTable).Header("[yellow]Top 5 by Level[/]"),
                new Panel(apTable).Header("[yellow]Top 5 by AP[/]")
            );

            AnsiConsole.Write(grid);
        }

        private void ShowGroupsSummary(List<RiotAccount> accounts)
        {
            var groups = accounts
                .Where(a => !string.IsNullOrEmpty(a.Group))
                .GroupBy(a => a.Group!)
                .OrderByDescending(g => g.Count());

            if (!groups.Any())
            {
                return;
            }

            AnsiConsole.Write(new Rule("[cyan]👥 Groups Summary[/]").LeftJustified());

            var groupsTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
            groupsTable.AddColumn("[cyan]Group[/]");
            groupsTable.AddColumn("[cyan]Accounts[/]").Centered();
            groupsTable.AddColumn("[cyan]Ready[/]").Centered();
            groupsTable.AddColumn("[cyan]Avg Level[/]").Centered();

            foreach (var group in groups)
            {
                var ready = group.Count(a => a.IsReadyForDaily);
                var avgLevel = group.Average(a => a.Level);
                groupsTable.AddRow(
                    group.Key,
                    group.Count().ToString(),
                    $"[green]{ready}[/]",
                    $"[magenta]{avgLevel:F1}[/]"
                );
            }

            AnsiConsole.Write(groupsTable);
        }

        private Color GetRankColor(string rank)
        {
            return rank.ToLower() switch
            {
                "radiant" => Color.Yellow,
                "immortal" => Color.Red,
                "ascendant" => Color.Green,
                "diamond" => Color.Blue,
                "platinum" => Color.Cyan1,
                "gold" => Color.Gold1,
                "silver" => Color.Grey,
                "bronze" => Color.Orange3,
                "iron" => Color.Grey42,
                _ => Color.White
            };
        }
    }
}
