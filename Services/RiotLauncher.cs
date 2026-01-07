using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using VAM.Models;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;

namespace VAM.Services
{
    public class RiotLauncher
    {
        private readonly AccountStorage _storage;
        private readonly InputSimulator _inputSimulator;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public RiotLauncher(AccountStorage storage)
        {
            _storage = storage;
            _inputSimulator = new InputSimulator();
        }

        public async Task<bool> LaunchAccount(RiotAccount account, bool openValorant = true)
        {
            var settings = _storage.GetSettings();
            
            // Check if Riot Client exists
            if (!File.Exists(settings.RiotClientPath))
            {
                AnsiConsole.MarkupLine("[red]✗ Riot Client not found![/]");
                AnsiConsole.MarkupLine($"[grey]Expected path: {settings.RiotClientPath}[/]");
                AnsiConsole.MarkupLine("[yellow]Please update the path in Settings.[/]");
                return false;
            }

            try
            {
                // Close existing Riot processes
                await AnsiConsole.Status()
                    .Spinner(Spectre.Console.Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("Closing existing Riot processes...", async ctx =>
                    {
                        await CloseRiotProcesses();
                        await Task.Delay(2000);
                    });

                // Decrypt password
                string password = EncryptionService.Decrypt(account.EncryptedPassword);

                // Launch Riot Client
                await AnsiConsole.Status()
                    .Spinner(Spectre.Console.Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green"))
                    .StartAsync($"Launching Riot Client for [cyan]{account.Username}[/]...", async ctx =>
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = settings.RiotClientPath,
                            Arguments = "--launch-product=valorant --launch-patchline=live",
                            UseShellExecute = true
                        };

                        Process.Start(startInfo);
                        await Task.Delay(5000); // Wait for client to fully start
                    });

                // Perform automated login using keyboard simulation
                bool loginSuccess = await AutomateLogin(account.Username, password);
                
                if (!loginSuccess)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠ Auto-login may have failed. Check the Riot Client.[/]");
                }

                // Wait for login to complete and Valorant to start
                await AnsiConsole.Status()
                    .Spinner(Spectre.Console.Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("Waiting for Valorant to launch...", async ctx =>
                    {
                        // Wait up to 60 seconds for Valorant to start
                        for (int i = 0; i < 60; i++)
                        {
                            if (IsValorantRunning())
                            {
                                AnsiConsole.MarkupLine("[green]✓ Valorant is running![/]");
                                break;
                            }
                            await Task.Delay(1000);
                        }
                    });

                await Task.Delay(settings.LoginDelayMs);

                // Update last played time
                account.LastPlayed = DateTime.Now;
                _storage.UpdateAccount(account);

                AnsiConsole.MarkupLine("[green]✓ Account launched successfully![/]");
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error launching account: {ex.Message}[/]");
                return false;
            }
        }

        public async Task CloseRiotProcesses()
        {
            var processNames = new[] { "RiotClientServices", "RiotClientUx", "VALORANT-Win64-Shipping", "VALORANT" };
            
            foreach (var name in processNames)
            {
                try
                {
                    foreach (var process in Process.GetProcessesByName(name))
                    {
                        process.Kill();
                        await Task.Delay(100);
                    }
                }
                catch { }
            }
        }

        public bool IsRiotClientRunning()
        {
            return Process.GetProcessesByName("RiotClientServices").Length > 0 ||
                   Process.GetProcessesByName("RiotClientUx").Length > 0;
        }

        public bool IsValorantRunning()
        {
            return Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0;
        }

        private async Task<bool> AutomateLogin(string username, string password)
        {
            try
            {
                IntPtr hwnd = IntPtr.Zero;
                
                await AnsiConsole.Status()
                    .Spinner(Spectre.Console.Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .StartAsync("Performing auto-login...", async ctx =>
                    {
                        // Wait for Riot Client window
                        ctx.Status("Waiting for Riot Client window...");
                        
                        for (int i = 0; i < 30; i++)
                        {
                            hwnd = FindRiotClientWindow();
                            
                            if (hwnd != IntPtr.Zero)
                            {
                                AnsiConsole.MarkupLine($"[green]✓ Found Riot Client window[/]");
                                break;
                            }
                                
                            await Task.Delay(500);
                        }

                        if (hwnd == IntPtr.Zero)
                        {
                            AnsiConsole.MarkupLine("[red]✗ Could not find Riot Client window[/]");
                            return;
                        }

                        // Bring window to front
                        SetForegroundWindow(hwnd);
                        await Task.Delay(500);

                        ctx.Status("Waiting for login form to load...");
                        await Task.Delay(3000); // Wait for login form

                        // The app opens with focus on username field
                        // Type username
                        ctx.Status("Entering username...");
                        _inputSimulator.Keyboard.TextEntry(username);
                        await Task.Delay(300);

                        // Tab to password field
                        ctx.Status("Moving to password field...");
                        _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        await Task.Delay(300);

                        // Type password
                        ctx.Status("Entering password...");
                        _inputSimulator.Keyboard.TextEntry(password);
                        await Task.Delay(300);

                        // Tab 7 times to reach login button
                        ctx.Status("Moving to login button...");
                        for (int i = 0; i < 7; i++)
                        {
                            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                            await Task.Delay(100);
                        }

                        // Press Enter to login
                        ctx.Status("Logging in...");
                        _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                        await Task.Delay(5000); // Wait for login to process
                    });

                AnsiConsole.MarkupLine("[green]✓ Login automation completed[/]");
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Automation error: {ex.Message}[/]");
                return false;
            }
        }

        private void ClickAt(int x, int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        private IntPtr FindRiotClientWindow()
        {
            IntPtr foundHwnd = IntPtr.Zero;

            // Method 1: Try finding by window title containing "Riot Client"
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return true;

                StringBuilder sb = new StringBuilder(length + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (title.Contains("Riot Client", StringComparison.OrdinalIgnoreCase))
                {
                    foundHwnd = hWnd;
                    return false; // Stop enumeration
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            if (foundHwnd != IntPtr.Zero)
                return foundHwnd;

            // Method 2: Find by process name
            var processNames = new[] { "RiotClientUx", "RiotClientServices" };
            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            return process.MainWindowHandle;
                        }
                    }
                    catch { }
                }
            }

            // Method 3: Try known class names
            var classNames = new[] { "RCLIENT", "Chrome_WidgetWin_1", "CEF-OSC-WIDGET" };
            foreach (var className in classNames)
            {
                var hwnd = FindWindow(className, null);
                if (hwnd != IntPtr.Zero)
                {
                    // Verify it's a Riot window by checking the process
                    GetWindowThreadProcessId(hwnd, out uint processId);
                    try
                    {
                        var process = Process.GetProcessById((int)processId);
                        if (process.ProcessName.Contains("Riot", StringComparison.OrdinalIgnoreCase))
                        {
                            return hwnd;
                        }
                    }
                    catch { }
                }
            }

            return IntPtr.Zero;
        }
    }
}
