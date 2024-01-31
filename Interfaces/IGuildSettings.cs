using IrisBot.Enums;
using Lavalink4NET.Rest;
using Lavalink4NET.Rest.Entities.Tracks;

namespace IrisBot.Interfaces
{
    public interface IGuildSettings
    {
        ulong GuildId { get; }
        float PlayerVolume { get; set; }
        ulong? ListMessagdId { get; set; }
        Translations Language { get; set; }
        TrackSearchMode SearchPlatform { get; set; }
        ulong? RoleMessageId { get; set; }
        List<string> RoleEmojiIds { get; set; }
        bool IsPrivateChannel { get; set; }
    }
}
