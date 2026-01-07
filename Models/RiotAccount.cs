using System;

namespace VAM.Models
{
    public class RiotAccount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string RiotId { get; set; } = string.Empty; // Format: name#tag (required)
        public string? DisplayName { get; set; }
        public string? Region { get; set; } = "EU";
        public int Level { get; set; } = 1;
        public int AP { get; set; } = 0;
        public string? Rank { get; set; } = "Unranked";
        public DateTime? LastPlayed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Active;
        
        // New fields for advanced features
        public bool IsFavorite { get; set; } = false;
        public string? Group { get; set; } // e.g., "Main", "Smurf", "Friends"
        public List<DateTime> PlayHistory { get; set; } = new List<DateTime>();
        public int TotalGamesPlayed { get; set; } = 0;
        public DateTime? LastApiSync { get; set; }
        
        // Calculated property for time since last played
        public string TimeSinceLastPlayed
        {
            get
            {
                if (LastPlayed == null) return "Never";
                var diff = DateTime.Now - LastPlayed.Value;
                if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }

        // Check if account is ready for daily mission (22+ hours)
        public bool IsReadyForDaily => LastPlayed == null || (DateTime.Now - LastPlayed.Value).TotalHours >= 22;

        // Hours until ready for daily
        public double HoursUntilReady
        {
            get
            {
                if (LastPlayed == null) return 0;
                var diff = 22 - (DateTime.Now - LastPlayed.Value).TotalHours;
                return diff > 0 ? diff : 0;
            }
        }
    }

    public enum AccountStatus
    {
        Active,
        Banned,
        Suspended,
        Inactive
    }
}
