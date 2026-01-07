using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Spectre.Console;

namespace VAM
{
    class Program
    {
        // Import Windows API for setting console icon
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        const uint WM_SETICON = 0x0080;
        const uint IMAGE_ICON = 1;
        const uint LR_LOADFROMFILE = 0x00000010;
        const uint ICON_SMALL = 0;
        const uint ICON_BIG = 1;

        static async Task Main(string[] args)
        {
            try
            {
                // Set console encoding for emoji support
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                
                // Set console icon
                SetConsoleIcon();
                
                // Set console title
                Console.Title = "V-AM | Valorant Account Manager";
                
                // Set fixed console size for consistent UI
                try
                {
                    // Standard size that fits most screens but allows for good UI layout
                    int width = 120;
                    int height = 35;
                    
                    // Set buffer first to avoid errors if window is smaller than buffer
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.SetWindowSize(width, height);
                        Console.SetBufferSize(width, height); // Fix buffer to window size to disable scroll bars if desired, or make height larger
                    }
                }
                catch { /* Ignore resizing errors on unsupported terminals */ }

                var app = new Application();
                await app.Run();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                AnsiConsole.MarkupLine("\n[red]An unexpected error occurred. Press any key to exit.[/]");
                Console.ReadKey(true);
            }
        }

        static void SetConsoleIcon()
        {
            try
            {
                var hwnd = GetConsoleWindow();
                if (hwnd == IntPtr.Zero) return;

                // Try to find the icon file
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app-icon.ico");
                if (!System.IO.File.Exists(iconPath))
                {
                    iconPath = "app-icon.ico";
                }
                
                if (System.IO.File.Exists(iconPath))
                {
                    var hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
                    if (hIcon != IntPtr.Zero)
                    {
                        SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hIcon);
                        SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, hIcon);
                    }
                }
            }
            catch
            {
                // Silently ignore icon setting errors
            }
        }
    }
}
