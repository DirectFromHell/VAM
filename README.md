<div align="center">
  <img src="Valorant Account Manager.png" alt="V-AM Logo" width="600"/>
  
  # V-AM (Valorant Account Manager)
  
  ### 🎮 A beautiful console application for managing multiple Valorant/Riot accounts
  
  [![Download](https://img.shields.io/badge/Download-Latest%20Release-success?style=for-the-badge&logo=google-drive)](https://drive.google.com/file/d/1KDjXJkIGu1qemlRNqRIA2tqVDNLjCCbq/view?usp=sharing)
  [![VirusTotal](https://img.shields.io/badge/VirusTotal-Clean-brightgreen?style=for-the-badge&logo=virustotal)](https://www.virustotal.com/gui/file/b5a15b7f491bed661001e8d5718b398a092bcf64d842bff4b66762ef5d67c9b4?nocache=1)
  [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
  [![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)

</div>

---

## ✨ Features

- 📋 **Account Management** - Add, edit, delete, and organize your Riot accounts
- 🔐 **Secure Storage** - Passwords are encrypted using Windows DPAPI
- 🚀 **Quick Launch** - One-click login to any account
- ⏰ **Daily Mission Tracker** - Know which accounts are ready for the 22-hour daily mission
- � **Search & Filter** - Find accounts by name, region, rank, status, or group
- 🚦 **Launch Queue** - Launch multiple accounts sequentially with customizable delays
- 📊 **Advanced Statistics** - Detailed analytics including activity charts, rank distribution, and top accounts
- ⭐ **Favorites & Groups** - Organize accounts with favorites and custom groups (Main, Smurf, etc.)
- 🔄 **API Integration** - Auto-fetch account data (level, rank) from unofficial Riot/Valorant APIs
- 🎨 **Beautiful UI** - Modern console interface with colors and emoji

## � Download & Installation

### 🚀 Quick Start - Download Ready-to-Use App

**[📦 Download V-AM.exe (Latest Release)](https://drive.google.com/file/d/1KDjXJkIGu1qemlRNqRIA2tqVDNLjCCbq/view?usp=sharing)**

**✅ Verified Safe:** [Check VirusTotal Scan Results](https://www.virustotal.com/gui/file/b5a15b7f491bed661001e8d5718b398a092bcf64d842bff4b66762ef5d67c9b4?nocache=1)

1. Download the executable from the link above
2. Run `V-AM.exe`
3. Start managing your accounts!

> **Note:** Windows may show a SmartScreen warning for unsigned applications. This is normal for new executables. Click "More info" → "Run anyway" to proceed.

---

### 🛠️ Build from Source

#### Prerequisites
- .NET 8.0 SDK or later
- Windows OS (for DPAPI encryption)

### Build from Source

```bash
# Clone or download the project
cd V-AM

# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### Create Executable

```bash
# Create single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Output will be in: bin/Release/net8.0/win-x64/publish/V-AM.exe
```

## 🚀 Usage

### Main Menu
- **View All Accounts** - See all your accounts in a table with status indicators
- **Add New Account** - Add a new Riot account (with optional API auto-fill)
- **Quick Launch** - Quickly launch an account (sorted by ready status)
- **Search & Filter** - Find accounts by name, region, rank, status, or group
- **Launch Queue** - Launch multiple accounts sequentially with delays
- **Advanced Statistics** - View detailed analytics, charts, and insights
- **Settings** - Configure paths and preferences

### Account Management
- View account details
- Edit account information
- Launch account (auto-login)
- Mark as played
- Toggle favorite status
- Set group (Main, Smurf, Friends, etc.)
- Sync with API (auto-update level and rank)
- Delete account

### Search & Filter
Find accounts easily:
- **Search by Name** - Search username, display name, or notes
- **Filter by Region** - EU, NA, AP, KR, LATAM, BR
- **Filter by Rank** - Unranked to Radiant
- **Filter by Status** - Active, Banned, Suspended, Inactive
- **Filter by Group** - Custom groups you've created
- **Show Favorites** - See only favorited accounts
- **Show Ready** - See only accounts ready for daily mission

### Launch Queue
Launch multiple accounts automatically:
- **Launch All Ready** - Queue all accounts ready for daily mission
- **Launch by Group** - Queue all accounts in a specific group
- **Launch Favorites** - Queue all favorited accounts
- **Custom Selection** - Choose specific accounts to queue
- **Configurable Delays** - Set delay between launches (1-60 minutes)

### Advanced Statistics
View detailed analytics:
- Overview panel with key metrics
- Activity analysis (last 24h, 7d, 30d+)
- Rank distribution chart
- Region distribution chart
- Top 5 accounts by level and AP
- Groups summary
- Total games played tracking

### API Integration
Auto-fetch account data from Riot/Valorant APIs:
- Account level
- Current rank (Competitive)
- Display name
- Region
- Account card

Simply enter your Riot ID (name#tag) when adding an account!

### Daily Mission Tracking
The app tracks when you last played on each account. After 22 hours, the account is marked as "Ready" for the daily mission that gives 1000 AP.

## ⚙️ Settings

Configure in the Settings menu:
- **Riot Client Path** - Path to RiotClientServices.exe
- **Valorant Path** - Path to VALORANT.exe
- **Login Delay** - Delay between launching client and game
- **Default Region** - Default region for new accounts
- **Auto Close Client** - Automatically close existing Riot processes
- **Auto Fetch Account Data** - Automatically fetch data from API when adding accounts
- **Queue Delay** - Default delay between accounts in queue (minutes)
- **Enable Notifications** - Show notifications for ready accounts
- **Default Sort** - Default sort order for accounts list

## 🔒 Security & Privacy

- ✅ **Verified Clean:** All releases are scanned by VirusTotal - [View Latest Scan](https://www.virustotal.com/gui/file/b5a15b7f491bed661001e8d5718b398a092bcf64d842bff4b66762ef5d67c9b4?nocache=1)
- 🔐 **Passwords are encrypted** using Windows Data Protection API (DPAPI)
- 💾 **Data is stored locally** in `%APPDATA%/V-AM/`
- 🚫 **No telemetry or tracking** - Your data never leaves your computer
- 🔓 **Open Source** - Review the code yourself
- 🌐 **Optional API calls** only to fetch public account info (Henrik API)
- ⚠️ **No authentication required** - API calls don't require your credentials

## 📁 Data Location

Your data is stored in:
```
%APPDATA%/V-AM/
├── accounts.json    # Encrypted account data
└── settings.json    # App settings
```

## 🎮 How Auto-Login Works

1. Closes any existing Riot/Valorant processes
2. Launches Riot Client with command-line credentials
3. Automatically logs into your account using keyboard simulation
4. Opens Valorant

## 🌐 API Integration

The app uses unofficial Valorant APIs to fetch account data:
- **Henrik API** (https://docs.henrikdev.xyz/) - For account info, rank, and stats
- No authentication required for basic queries
- Rate-limited - use responsibly
- Based on community-maintained APIs

## ⚠️ Disclaimer

This tool is for **personal use only**. Use at your own risk. The developers are not responsible for any account actions taken by Riot Games. Using third-party tools may violate Riot's Terms of Service.

**Important:** This application does NOT modify game files or provide any in-game advantages. It's simply an account management and launcher tool.

---

## 🤝 Contributing

Contributions are welcome! Feel free to:
- Report bugs
- Suggest new features
- Submit pull requests
- Improve documentation

## 📝 License

MIT License - Feel free to modify and distribute.

---

<div align="center">
  
### Made with ❤️ for Valorant account managers

**[⬇️ Download Now](https://drive.google.com/file/d/1KDjXJkIGu1qemlRNqRIA2tqVDNLjCCbq/view?usp=sharing)** | **[🛡️ VirusTotal Scan](https://www.virustotal.com/gui/file/b5a15b7f491bed661001e8d5718b398a092bcf64d842bff4b66762ef5d67c9b4?nocache=1)**

</div>
