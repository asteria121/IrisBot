using Discord.WebSocket;
using Lavalink4NET.Players.Queued;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrisBot.Player
{
    public sealed record class IrisPlayerOptions : QueuedLavalinkPlayerOptions
    {
        public readonly ISocketMessageChannel Channel;

        public IrisPlayerOptions(ISocketMessageChannel channel)
        {
            Channel = channel;
            SelfDeaf = true;
        }
    }
}
