using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IrisBot.Database;
using IrisBot.Enums;
using IrisBot.Player;
using IrisBot.Translation;
using Lavalink4NET;
using Lavalink4NET.Events;
using Lavalink4NET.Tracks;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Rest;
using Lavalink4NET.Tracking;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players.Preconditions;
using Microsoft.Extensions.Options;
using System.Text;
using System.Collections.Immutable;
using Discord.Commands;
using Microsoft.Extensions.Http;

namespace IrisBot.Modules
{
    [Discord.Interactions.Group("playlist", "Playlist management command")]
    public class PlaylistCommandModule : InteractionModuleBase<ShardedInteractionContext>
    {
        private IAudioService _audioService;
        private IServiceProvider _services;

        public PlaylistCommandModule(IAudioService audioService, IServiceProvider services)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _services = services;
        }

        [SlashCommand("list", "Display playlist lists")]
        [Discord.Interactions.RequireBotPermission(GuildPermission.EmbedLinks)]
        [Discord.Interactions.RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ViewPlaylist()
        {
            await DeferAsync(ephemeral: true).ConfigureAwait(false);
            string path = Path.Combine(Program.PlaylistDirectory, Context.Guild.Id.ToString());
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            if (!Directory.Exists(path))
            {
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("no_playlist", lang), ephemeral: true).ConfigureAwait(false);
                return;
            }

            DirectoryInfo di = new DirectoryInfo(path);
            StringBuilder sb = new StringBuilder();
            sb.Append("```");
            int i = 1;
            foreach (FileInfo file in di.GetFiles())
            {
                sb.AppendLine($"{i} - {file.Name}");
            }
            sb.AppendLine("```");

            await FollowupAsync(sb.ToString(), ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("add", "Make or \"OVERWRITE\" playlist")]
        [Discord.Interactions.RequireBotPermission(GuildPermission.EmbedLinks)]
        [Discord.Interactions.RequireBotPermission(GuildPermission.SendMessages)]
        public async Task AddPlaylist(string name)
        {
            await DeferAsync().ConfigureAwait(false);
            SocketGuildUser user = (SocketGuildUser)Context.User;
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            if (player == null || (player.Queue.IsEmpty && player.CurrentTrack == null))
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("empty_queue", lang)).ConfigureAwait(false);
            }
            else if (player.VoiceChannelId != user.VoiceChannel.Id)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("different_channel_warning", lang)).ConfigureAwait(false);
            }
            else
            {
                PlaylistResult result = await Playlist.CreatePlaylistAsync(new Playlist(player.GuildId, name), player.CurrentTrack, player.Queue).ConfigureAwait(false);
                switch (result)
                {
                    case PlaylistResult.New:
                        await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("playlist_new", lang)}: {name}").ConfigureAwait(false);
                        break;
                    case PlaylistResult.Overwrite:
                        await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("playlist_overwrite", lang)}: {name}").ConfigureAwait(false);
                        break;
                    case PlaylistResult.CreationLimit:
                        await FollowupAsync(await TranslationLoader.GetTranslationAsync("playlist_creation_limit", lang)).ConfigureAwait(false);
                        break;
                    case PlaylistResult.Fail:
                        await FollowupAsync(await TranslationLoader.GetTranslationAsync("playlist_fail", lang)).ConfigureAwait(false);
                        break;
                }
            }
        }

        [SlashCommand("remove", "Remove specified name of playlist")]
        [Discord.Interactions.RequireBotPermission(GuildPermission.EmbedLinks)]
        [Discord.Interactions.RequireBotPermission(GuildPermission.SendMessages)]
        public async Task RemovePlaylist(string name)
        {
            await DeferAsync().ConfigureAwait(false);
            PlaylistDeleteResult result = await Playlist.DeletePlaylistAsync(Context.Guild.Id, name).ConfigureAwait(false);
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            switch (result)
            {
                case PlaylistDeleteResult.Success:
                    await FollowupAsync(await TranslationLoader.GetTranslationAsync("playlist_remove_success", lang)).ConfigureAwait(false);
                    break;
                case PlaylistDeleteResult.NotExists:
                    await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("playlist_remove_not_exists", lang)}: {name}").ConfigureAwait(false);
                    break;
                case PlaylistDeleteResult.Fail:
                    await FollowupAsync(await TranslationLoader.GetTranslationAsync("playlist_remove_fail", lang)).ConfigureAwait(false);
                    break;
            }
        }

        [SlashCommand("load", "Load specified name of playlist")]
        [Discord.Interactions.RequireBotPermission(GuildPermission.EmbedLinks)]
        [Discord.Interactions.RequireBotPermission(GuildPermission.SendMessages)]
        public async Task LoadPlaylist(string name)
        {
            await DeferAsync().ConfigureAwait(false);

            SocketGuildUser user = (SocketGuildUser)Context.User;
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            string[]? lists = await Playlist.LoadPlaylistAsync(Context.Guild.Id, name);

            if (lists == null || lists.Count() < 1)
            {
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("playlist_empty_error", lang)).ConfigureAwait(false);
                return;
            }

            var preconditions = ImmutableArray.Create(item: PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.Queue.Count < Program.MaxQueueCount));
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, connectToVoiceChannel: true, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            int totalPlayedSong = 0;
            List<string> failedSongs = new List<string>();
            foreach (var trackUri in lists)
            {
                if (player.Queue.Count() >= Program.MaxQueueCount)
                    break;

                TrackLoadResult searchResult = await _audioService.Tracks.LoadTracksAsync(trackUri, loadOptions: default).ConfigureAwait(false);
                if (searchResult.IsSuccess)
                {
                    TrackReference reference = new TrackReference(searchResult.Track);
                    IrisTrack track = new IrisTrack(reference);
                    track.Requester = user.Username;
                    await player.PlayAsync(track).ConfigureAwait(false);
                    totalPlayedSong++;
                }
                else
                {
                    failedSongs.Add(trackUri);
                }
            }

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle(await TranslationLoader.GetTranslationAsync("added_playlist_queue", lang))
                .WithDescription($"{await TranslationLoader.GetTranslationAsync("custom_playlist", lang)}: {name}" +
                    $"\r\n{await TranslationLoader.GetTranslationAsync("total_tracks", lang)} : `{totalPlayedSong}/{lists.Count()}`")
                .WithColor(Color.Purple);

            // 불러오기 실패한 항목은 사용자에게 알림
            if (failedSongs.Count() > 0)
            {
                StringBuilder sb = new StringBuilder();
                int count = 0;
                foreach (var link in failedSongs)
                {
                    sb.AppendLine(link);
                    count++;
                    if (count >= 10) break;
                }

                eb.AddField(await TranslationLoader.GetTranslationAsync("playlist_link_error", lang), sb.ToString());
            }

            await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
        }
    }
}