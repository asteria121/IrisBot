﻿using IrisBot.Enums;
using Lavalink4NET.Rest;

namespace IrisBot.Interfaces
{
    public interface IGuildSettings
    {
        ulong GuildId { get; }
        float PlayerVolume { get; set; }
        ulong? ListMessagdId { get; set; }
        Translations Language { get; set; }
        SearchMode SearchPlatform { get; set; }
        ulong? RoleMessageId { get; set; }
        List<string> RoleEmojiIds { get; set; }
        bool IsPrivateChannel { get; set; }
    }
}
