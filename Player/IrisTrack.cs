using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;

namespace IrisBot.Player
{
    public sealed record class IrisTrack(TrackReference Reference) : ITrackQueueItem
    {
        public string Author;
        public string Title;
        public string Requester;
    }
}
