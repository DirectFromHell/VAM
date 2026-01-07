using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using VAM.Models;

namespace VAM.Services
{
    /// <summary>
    /// Queue system for launching multiple accounts sequentially
    /// </summary>
    public class LaunchQueueService
    {
        private readonly AccountStorage _storage;
        private readonly RiotLauncher _launcher;
        private bool _isRunning = false;
        private CancellationTokenSource? _cancellationTokenSource;

        public LaunchQueueService(AccountStorage storage, RiotLauncher launcher)
        {
            _storage = storage;
            _launcher = launcher;
        }

        public bool IsRunning => _isRunning;

        /// <summary>
        /// Launch multiple accounts sequentially - waits for Valorant to close before next account
        /// </summary>
        public async Task LaunchQueueAsync(List<RiotAccount> accounts)
        {
            if (_isRunning)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Queue is already running![/]");
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                AnsiConsole.MarkupLine($"[green]🚀 Starting queue with {accounts.Count} account(s)[/]");
                AnsiConsole.MarkupLine("[cyan]📌 The next account will launch automatically when you close Valorant[/]");
                AnsiConsole.WriteLine();

                for (int i = 0; i < accounts.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        AnsiConsole.MarkupLine("[yellow]⏸️  Queue cancelled by user[/]");
                        break;
                    }

                    var account = accounts[i];
                    
                    AnsiConsole.MarkupLine($"[cyan]═══════════════════════════════════════[/]");
                    AnsiConsole.MarkupLine($"[bold white]Account {i + 1}/{accounts.Count}: {account.Username}[/]");
                    AnsiConsole.MarkupLine($"[cyan]═══════════════════════════════════════[/]");
                    
                    // Launch the account
                    bool success = await _launcher.LaunchAccount(account);
                    
                    if (success)
                    {
                        AnsiConsole.MarkupLine($"[green]✓ Successfully launched {account.Username}[/]");
                        
                        // Wait for next account (except for last one)
                        if (i < accounts.Count - 1)
                        {
                            AnsiConsole.WriteLine();
                            AnsiConsole.MarkupLine($"[yellow]⏳ Waiting for Valorant to close before launching next account...[/]");
                            AnsiConsole.MarkupLine($"[grey]   Next: {accounts[i + 1].Username}[/]");
                            
                            // Wait for Valorant to close
                            await WaitForValorantToClose(token);
                            
                            if (!token.IsCancellationRequested)
                            {
                                AnsiConsole.MarkupLine("[green]✓ Valorant closed! Launching next account...[/]");
                                await Task.Delay(2000); // Small delay before next account
                            }
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Failed to launch {account.Username}[/]");
                        
                        // Ask if user wants to continue with next account
                        if (i < accounts.Count - 1)
                        {
                            if (!AnsiConsole.Confirm("[yellow]Continue with next account?[/]"))
                            {
                                break;
                            }
                        }
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓ Queue completed![/]");
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]⏸️  Queue was cancelled[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Queue error: {ex.Message}[/]");
            }
            finally
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Wait for Valorant process to close
        /// </summary>
        private async Task WaitForValorantToClose(CancellationToken token)
        {
            // First, wait a bit for Valorant to fully start
            await Task.Delay(5000, token);
            
            // Now monitor for Valorant to close
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync("[yellow]🎮 Valorant is running... Close the game to continue[/]", async ctx =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        bool valorantRunning = IsValorantRunning();
                        
                        if (!valorantRunning)
                        {
                            // Double-check after a short delay (in case game is restarting)
                            await Task.Delay(3000, token);
                            if (!IsValorantRunning())
                            {
                                break; // Valorant is truly closed
                            }
                        }
                        
                        await Task.Delay(2000, token); // Check every 2 seconds
                    }
                });
        }

        private bool IsValorantRunning()
        {
            return Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0;
        }

        public void CancelQueue()
        {
            if (_isRunning && _cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                AnsiConsole.MarkupLine("[yellow]Cancelling queue...[/]");
            }
        }

        /// <summary>
        /// Launch all ready accounts
        /// </summary>
        public async Task LaunchAllReadyAccountsAsync()
        {
            var readyAccounts = _storage.GetReadyAccounts()
                .OrderByDescending(a => a.IsFavorite)
                .ThenBy(a => a.LastPlayed)
                .ToList();

            if (!readyAccounts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]⚠ No accounts are ready for daily mission[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Found {readyAccounts.Count} ready account(s):[/]");
            foreach (var acc in readyAccounts)
            {
                AnsiConsole.MarkupLine($"  • {acc.Username} {(acc.IsFavorite ? "⭐" : "")}");
            }
            AnsiConsole.WriteLine();

            if (AnsiConsole.Confirm("[yellow]Launch all these accounts?[/]"))
            {
                await LaunchQueueAsync(readyAccounts);
            }
        }

        /// <summary>
        /// Launch accounts by group
        /// </summary>
        public async Task LaunchGroupAsync(string groupName)
        {
            var groupAccounts = _storage.FilterByGroup(groupName)
                .Where(a => a.Status == AccountStatus.Active)
                .OrderByDescending(a => a.IsReadyForDaily)
                .ThenBy(a => a.LastPlayed)
                .ToList();

            if (!groupAccounts.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]⚠ No accounts found in group '{groupName}'[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[cyan]Group '{groupName}' has {groupAccounts.Count} account(s)[/]");
            await LaunchQueueAsync(groupAccounts);
        }
    }
}
