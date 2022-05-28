using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dharma_DSharp.Models
{
    public class AllianceMember
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong DiscordUserId { get; set; }
        public string DiscordUserName { get; set; } = string.Empty;
        public string DiscordDisplayName { get; set; } = string.Empty;
        public string PhantasyUserId { get; set; } = string.Empty;
        public DateTime LastActivityUpdate { get; set; }
        public string Notes { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"ID: {DiscordUserId}, UserName: {DiscordUserName}, DisplayName: {DiscordDisplayName}, PSO-Id: {PhantasyUserId}";
        }

        public static bool operator ==(AllianceMember member, AllianceMember other)
        {
            return member.DiscordUserId == other.DiscordUserId;
        }

        public static bool operator !=(AllianceMember member, AllianceMember other)
        {
            return member.DiscordUserId != other.DiscordUserId;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (AllianceMember)obj;
            return DiscordUserId == other.DiscordUserId;
        }
    }
}
