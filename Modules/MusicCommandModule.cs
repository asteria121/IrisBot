using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IrisBot.Database;
using IrisBot.Enums;
using IrisBot.Player;
using IrisBot.Translation;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;
using System.Text;

namespace IrisBot.Modules
{
    public class MusicCommandModule : InteractionModuleBase<ShardedInteractionContext>, IHostedService
    {
        private IAudioService _audioService;

        public MusicCommandModule(IAudioService audioService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _audioService.TrackEnded += _audioService_TrackEnd;
            _audioService.TrackStarted += _audioService_TrackStarted;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _audioService.TrackEnded -= _audioService_TrackEnd;
            _audioService.TrackStarted -= _audioService_TrackStarted;
            return Task.CompletedTask;
        }

        private async Task _audioService_TrackStarted(object sender, TrackStartedEventArgs eventArgs)
        {
            IrisPlayer? player = eventArgs.Player as IrisPlayer;

            if (player?.CurrentItem != null)
            {
                IrisTrack? track = (IrisTrack?)player.CurrentItem;

                if (!string.IsNullOrEmpty(track?.Requester))
                {
                    string requesterName = track.Requester;
                    EmbedBuilder eb = new EmbedBuilder();
                    Translations lang = await TranslationLoader.FindGuildTranslationAsync(player.GuildId).ConfigureAwait(false);
                    eb.WithTitle(await TranslationLoader.GetTranslationAsync("now_playing", lang))
                        .WithDescription($"[{player.CurrentTrack?.Title}]({player.CurrentTrack?.Uri?.ToString()})" +
                            $"\r\n{await TranslationLoader.GetTranslationAsync("author", lang)} : `{player.CurrentTrack?.Author}`" +
                            $"\r\n{await TranslationLoader.GetTranslationAsync("duration", lang)} : `{player.CurrentTrack?.Duration.ToString(@"hh\:mm\:ss")}`" +
                            $"\r\n{await TranslationLoader.GetTranslationAsync("requester_name", lang)} : `{requesterName}`")
                        .WithColor(Color.Purple);

                    await player.Channel.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
                }
            }
        }

        private async Task _audioService_TrackEnd(object sender, TrackEndedEventArgs eventArgs)
        {
            IrisPlayer? player = eventArgs.Player as IrisPlayer;
            if (eventArgs.Reason != Lavalink4NET.Protocol.Payloads.Events.TrackEndReason.Finished)
                return;

            if (player?.Channel != null && player.Queue.IsEmpty)
            {
                Translations lang = await TranslationLoader.FindGuildTranslationAsync(player.GuildId).ConfigureAwait(false);
                await player.Channel.SendMessageAsync(await TranslationLoader.GetTranslationAsync("track_end_no_queue", lang)).ConfigureAwait(false);
            }
        }

        [SlashCommand("join", "Join voicechannel", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task JoinAsync()
        {
            await DeferAsync().ConfigureAwait(false);
            
            // 이미 봇을 사용중인 경우 사용 불가
            var preconditions = ImmutableArray.Create(item: PlayerPrecondition.NotPlaying);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, connectToVoiceChannel: true, requireSameChannel: false, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            SocketGuildUser user = (SocketGuildUser)Context.User;

            if (player.VoiceChannelId != user.VoiceChannel.Id)
            {
                // 아무것도 재생중이지 않을 때에는 precondition으로 필터링 되지 않음
                await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("player_already_running", lang)}").ConfigureAwait(false);
            }
            else
            {
                await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("connect_voicechannel", lang)}: {user.VoiceChannel.Name}").ConfigureAwait(false);
            }
        }

        [SlashCommand("music", "Search music and add it to queue", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task MusicAsync(string query)
        {
            RequestOptions options = new RequestOptions();
            options.Timeout = 1; // milliseconds
            await DeferAsync(ephemeral: true, options);

            // 대기열이 가득 찬 경우 사용 불가
            var preconditions = ImmutableArray.Create(item: PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.Queue.Count < Program.MaxQueueCount));
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, connectToVoiceChannel: true, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            SocketGuildUser user = (SocketGuildUser)Context.User;
            GuildSettings? data = GuildSettings.GetGuildsList().Find(x => x.GuildId == Context.Guild.Id);

            // 설정된 볼륨으로 초기화
            if (data == null)
                await player.SetVolumeAsync(0.5f).ConfigureAwait(false);
            else
                await player.SetVolumeAsync(data.PlayerVolume).ConfigureAwait(false);

            TrackSearchMode mode = GuildSettings.FindGuildSearchMode(Context.Guild.Id);
            var searchResult = await _audioService.Tracks.LoadTracksAsync(query, mode);

            if (searchResult.Tracks.Length == 0)
            {
                // 결과가 0개인 경우 검색 실패로 판단한다.
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("search_failed", lang), ephemeral: true).ConfigureAwait(false);
            }
            else if (searchResult.IsPlaylist && searchResult.Tracks.Count() > 0)
            {
                // Playlist 정보가 있을 경우 플레이리스트 등록 메소드 사용
                foreach (var track in searchResult.Tracks)
                {
                    if (player.Queue.Count() >= Program.MaxQueueCount)
                        break;

                    IrisTrack irisTrack = new IrisTrack(new TrackReference(track));
                    irisTrack.Requester = user.Username;

                    await player.PlayAsync(irisTrack);
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle(await TranslationLoader.GetTranslationAsync("added_playlist_queue", lang))
                    .WithDescription($"[{searchResult.Playlist.Name}]({query})" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("total_tracks", lang)} : `{searchResult.Tracks.Length}`")
                    .WithColor(Color.Purple);

                // 대기열 수 초과시 Footer로 안내 메세지 출력
                if (player.Queue.Count() >= Program.MaxQueueCount)
                    eb.WithFooter(await TranslationLoader.GetTranslationAsync("playlist_fail_maximum_queue", lang));

                await FollowupAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
            }
            else if (searchResult.Tracks.Length == 1)
            {
                TrackReference reference = new TrackReference(searchResult.Tracks.First());
                IrisTrack track = new IrisTrack(reference);
                track.Requester = user.Username;

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle(await TranslationLoader.GetTranslationAsync("added_queue", lang))
                    .WithDescription($"[{reference.Track?.Title}]({reference.Track?.Uri?.ToString()})" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("author", lang)} : `{reference.Track?.Author}`" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("duration", lang)} : `{reference.Track?.Duration.ToString(@"hh\:mm\:ss")}`")
                    .WithColor(Color.Purple);

                await player.PlayAsync(track).ConfigureAwait(false);
                await FollowupAsync(embed: eb.Build(), ephemeral: true).ConfigureAwait(false);
            }
            else
            {
                string customId = "";
                StringBuilder sb = new StringBuilder(1024);
                if (mode == TrackSearchMode.YouTube)
                {
                    customId = "music_select";
                    sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("searchresult_youtube", lang)} \"{query}\"\r\n");
                }
                else
                {
                    customId = "music_select_soundcloud";
                    sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("searchresult_soundcloud", lang)} \"{query}\"\r\n");
                }

                var menu = new SelectMenuBuilder()
                {
                    CustomId = customId,
                    Placeholder = await TranslationLoader.GetTranslationAsync("select_number", lang),
                };

                for (int i = 0; i < 10 && i < searchResult.Tracks.Length; i++)
                {
                    LavalinkTrack track = searchResult.Tracks.ElementAt(i);
                    sb.AppendLine($"{i + 1}. {track.Title} - [{track.Author}] [{track.Duration}]");
                    menu.AddOption((i + 1).ToString(), track.Uri?.ToString(), $"{track.Uri?.ToString()}");
                }
                var component = new ComponentBuilder();
                component.WithSelectMenu(menu);
                sb.AppendLine("```");

                await FollowupAsync(sb.ToString(), components: component.Build(), ephemeral: true).ConfigureAwait(false);
            }
        }

        [SlashCommand("pause", "Pause current track", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task PauseAsync()
        {
            await DeferAsync().ConfigureAwait(false);

            // 재생중인 음악이 없을 경우 사용 불가, 현재 재생중인 상태에서만 사용 가능
            var preconditions = ImmutableArray.Create(PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.CurrentTrack != null), PlayerPrecondition.NotPaused);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            await player.PauseAsync();
            await FollowupAsync(await TranslationLoader.GetTranslationAsync("pause_music", lang));
        }

        [SlashCommand("resume", "Resume paused track", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ResumeAsync()
        {
            await DeferAsync().ConfigureAwait(false);

            // 재생중인 음악이 없을 경우 사용 불가, 일시정지 상태에서만 사용 가능
            var preconditions = ImmutableArray.Create(PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.CurrentTrack != null), PlayerPrecondition.Paused);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            await player.ResumeAsync();
            await FollowupAsync(await TranslationLoader.GetTranslationAsync("resume_music", lang));
        }

        [SlashCommand("seek", "Seek current track position", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task SeekAsync(int seconds)
        {
            await DeferAsync().ConfigureAwait(false);

            // 재생중인 음악이 없을 경우 사용 불가
            var preconditions = ImmutableArray.Create(PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.CurrentTrack != null));
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            await player.SeekAsync(time);
            await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("seek_to_position", lang)}: {new DateTime(time.Ticks).ToString("mm:ss")}");
        }

        [SlashCommand("volume", "Set player volume", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task VolumeAsync(int volume)
        {
            await DeferAsync().ConfigureAwait(false);

            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);

            if (volume < 1 || volume > 100)
            {
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("volume_invalid_value", lang));
            }
            else
            {
                await player.SetVolumeAsync((float)volume / 100).ConfigureAwait(false);
                await GuildSettings.UpdateVolumeAsync((float)volume / 100, Context.Guild.Id).ConfigureAwait(false);
                await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("volume_changed", lang)}: {volume}").ConfigureAwait(false);
            }
        }

        [SlashCommand("leave", "Leave voice channel", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task LeaveAsync()
        {
            await DeferAsync().ConfigureAwait(false);

            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            await player.DisconnectAsync().ConfigureAwait(false);
            await player.DisposeAsync().ConfigureAwait(false);
            await FollowupAsync(await TranslationLoader.GetTranslationAsync("disconnect_voicechannel", lang)).ConfigureAwait(false);
        }

        [SlashCommand("searchmode", "Change search platform YouTube or SoundCloud", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task Language(IrisSearchMode searchMode)
        {
            await DeferAsync().ConfigureAwait(false);
            await GuildSettings.UpdateSearchModeAsync(searchMode == IrisSearchMode.YouTube ? TrackSearchMode.YouTube : TrackSearchMode.SoundCloud, Context.Guild.Id).ConfigureAwait(false);
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);

            if (searchMode == IrisSearchMode.YouTube)
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("searchmode_youtube", lang)).ConfigureAwait(false);
            else
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("searchmode_soundcloud", lang)).ConfigureAwait(false);
        }

        [SlashCommand("list", "Display current track and queue list", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ListAsync()
        {
            await DeferAsync(ephemeral: true);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, requireSameChannel: false).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            StringBuilder sb = new StringBuilder();
            if (player?.CurrentTrack == null)
            {
                sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("now_playing", lang)}: N/A```");
            }
            else
            {
                int bitrate = 0;
                var channel = Context.Guild.GetVoiceChannel((ulong)player.VoiceChannelId);
                if (channel != null)
                    bitrate = channel.Bitrate / 1000;

                sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("now_playing", lang)}: {player.CurrentTrack.Title} - {player.CurrentTrack.Author} " +
                    $"[{player.Position?.RelativePosition.ToString(@"hh\:mm\:ss")}/{player.CurrentTrack.Duration.ToString(@"hh\:mm\:ss")}] " +
                    $"[{bitrate} Kbps]```");
                // Now playing: Song name - [AuthorName] [00:00:00/01:10:23] [96 Kbps]
            }

            ComponentBuilder? component = null;
            if (player?.Queue == null || player.Queue.Count == 0)
            {
                sb.AppendLine("```Nothing in queue.```");
            }
            else
            {
                int pageCount = 0;
                for (int i = 0; i < player.Queue.Count; pageCount++, i += Program.PagelistCount) ;
                // SelectMenu에 표시할 페이지 수 계산

                var menu = new SelectMenuBuilder()
                {
                    CustomId = "list_pageview",
                    Placeholder = await TranslationLoader.GetTranslationAsync("select_number", lang),
                };
                for (int i = 0; i < pageCount; i++)
                    menu.AddOption((i + 1).ToString(), (i + 1).ToString());

                component = new ComponentBuilder();
                component.WithSelectMenu(menu);

                sb.Append("```");

                for (int i = 0; i < Program.PagelistCount && i < player.Queue.Count; i++)
                {
                    IrisTrack myTrack = (IrisTrack)player.Queue.ElementAt(i);

                    sb.AppendLine($"{i + 1}. {myTrack.Reference.Track?.Title} - [{myTrack.Reference.Track?.Author}] [{myTrack.Reference.Track?.Duration.ToString(@"hh\:mm\:ss")}]" +
                        $" - [{await TranslationLoader.GetTranslationAsync("requester_name", lang)}: {myTrack.Requester}]");
                    // 1. Song name - [AuthorName] [01:10:23] - 신청자명
                }
                sb.AppendLine($"\r\nPage 1/{pageCount}```");
            }

            await FollowupAsync(sb.ToString(), components: component?.Build() ?? null, ephemeral: true).ConfigureAwait(false);
        }

        [SlashCommand("skip", "Skip current track", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task SkipAsync()
        {
            await DeferAsync().ConfigureAwait(false);

            // 재생중인 음악이 없을 경우 사용 불가
            var preconditions = ImmutableArray.Create(PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.CurrentTrack != null));
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            await player.SkipAsync().ConfigureAwait(false);
            await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("skip_music", lang)}: {player.CurrentTrack?.Title}").ConfigureAwait(false);
        }

        [SlashCommand("shuffle", "Randomize the entire queue", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ShuffleAsync()
        {
            await DeferAsync().ConfigureAwait(false);

            // 대기열 없는 경우 사용 불가
            var preconditions = ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            await player.Queue.ShuffleAsync().ConfigureAwait(false);
            await FollowupAsync(await TranslationLoader.GetTranslationAsync("shuffle_queue", lang)).ConfigureAwait(false);
        }

        [SlashCommand("remove", "Remove a track on the queue", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task RemoveAsync(int index)
        {
            await DeferAsync().ConfigureAwait(false);

            // 대기열 없는 경우 사용 불가
            var preconditions = ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            if (index < 1)
            {
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("queue_delete_overflow", lang)).ConfigureAwait(false);
            }
            else
            {
                if (index > player.Queue.Count)
                {
                    await FollowupAsync(await TranslationLoader.GetTranslationAsync("queue_delete_overflow", lang)).ConfigureAwait(false);
                }
                else
                {
                    LavalinkTrack? myTrack = player.Queue.ElementAt(index - 1).Track;
                    await player.Queue.RemoveAtAsync(index - 1).ConfigureAwait(false);
                    await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("remove_queue", lang)}: " +
                        $"{myTrack?.Title} - [{myTrack?.Author}] [{myTrack?.Duration.ToString(@"hh\:mm\:ss")}]").ConfigureAwait(false);
                    // Song name - [AuthorName] [01:10:23]
                }
            }
        }

        [SlashCommand("mremove", "Remove multiple tracks on the queue", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task MremoveAsync(int index, int count)
        {
            await DeferAsync().ConfigureAwait(false);

            // 대기열 없는 경우 사용 불가
            var preconditions = ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            if (index < 1 || count < 1)
            {
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("queue_delete_overflow", lang)).ConfigureAwait(false);
            }
            else
            {
                if (index + count - 1 > player.Queue.Count)
                {
                    await FollowupAsync(await TranslationLoader.GetTranslationAsync("queue_delete_overflow", lang)).ConfigureAwait(false);
                }
                else
                {
                    await player.Queue.RemoveRangeAsync(index - 1, count).ConfigureAwait(false);
                    await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("remove_queue", lang)}: " +
                        $"{await TranslationLoader.GetTranslationAsync("queue", lang)} #{index} ~ #{index + count - 1}").ConfigureAwait(false);
                    // 대기열 #1 ~ #5
                }
            }
        }

        [SlashCommand("clear", "Clear entire queue", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ClearAsync()
        {
            await DeferAsync().ConfigureAwait(false);

            // 대기열 없는 경우 사용 불가
            var preconditions = ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty);
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            await player.Queue.ClearAsync().ConfigureAwait(false);
            await FollowupAsync($"{await TranslationLoader.GetTranslationAsync("clear_queue", lang)}:");
        }

        [SlashCommand("loop", "Toggle loop mode", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task LoopAsync(IrisLoopMode loopMode)
        {
            await DeferAsync().ConfigureAwait(false);

            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            player.RepeatMode = (TrackRepeatMode)loopMode;
            if (loopMode == IrisLoopMode.None)
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("loop_none", lang)).ConfigureAwait(false);
            else if (loopMode == IrisLoopMode.Queue)
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("loop_entire", lang)).ConfigureAwait(false);
            else
                await FollowupAsync(await TranslationLoader.GetTranslationAsync("loop_single", lang)).ConfigureAwait(false);
        }

        /* // TODO: Lavalink4NET v4에 구현되지 않았음
        [SlashCommand("musictop", "Add track as 1st priority", runMode: RunMode.Async)]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task MusicTopAsync(string query)
        {
            await DeferAsync().ConfigureAwait(false);

            // 대기열이 가득 찬 경우 사용 불가
            var preconditions = ImmutableArray.Create(item: PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.Queue.Count < Program.MaxQueueCount));
            var player = await IrisPlayer.GetPlayerAsync(Context, _audioService, connectToVoiceChannel: true).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id).ConfigureAwait(false);
            SocketGuildUser user = (SocketGuildUser)Context.User;
            GuildSettings? data = GuildSettings.GetGuildsList().Find(x => x.GuildId == Context.Guild.Id);

            // 설정된 볼륨으로 초기화
            if (data == null)
                await player.SetVolumeAsync(0.5f).ConfigureAwait(false);
            else
                await player.SetVolumeAsync(data.PlayerVolume).ConfigureAwait(false);

            TrackSearchMode mode = GuildSettings.FindGuildSearchMode(Context.Guild.Id);
           
            var searchResult = await _audioService.Tracks.LoadTracksAsync(query, mode).ConfigureAwait(false);

            if (searchResult.Tracks.Length == 0)
            {
                // 결과가 0개인 경우 검색 실패로 판단한다.
                await RespondAsync(await TranslationLoader.GetTranslationAsync("search_failed", lang));
            }
            else if (searchResult.Tracks.Length == 1)
            {
                // 존재하는 1개의 링크를 입력할 경우 바로 재생한다
                TrackReference reference = new TrackReference(searchResult.Tracks.First());
                IrisTrack track = new IrisTrack(reference);
                track.Requester = user.Username;

                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle(await TranslationLoader.GetTranslationAsync("top_priority_queue", lang))
                    .WithDescription($"[{track.Reference.Track?.Title}]({track.Reference.Track?.Uri?.ToString()})" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("author", lang)} : `{track.Reference.Track?.Author}`" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("duration", lang)} : `{track.Reference.Track?.Duration.ToString(@"hh\:mm\:ss")}`")
                    .WithColor(Color.Purple);

                await player.PlayTopAsync(track).ConfigureAwait(false);
                await FollowupAsync(embed: eb.Build()).ConfigureAwait(false);
            }
            else
            {
                string customId = "";
                StringBuilder sb = new StringBuilder(1024);
                if (mode == TrackSearchMode.YouTube)
                {
                    customId = "music_select_top";
                    sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("searchresult_youtube", lang)} \"{query}\"\r\n");
                }
                else
                {
                    customId = "music_select_soundcloud_top";
                    sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("searchresult_soundcloud", lang)} \"{query}\"\r\n");
                }

                var menu = new SelectMenuBuilder()
                {
                    CustomId = customId,
                    Placeholder = await TranslationLoader.GetTranslationAsync("select_number", lang),
                };

                for (int i = 0; i < 10 && i < searchResult.Tracks.Length; i++)
                {
                    LavalinkTrack track = searchResult.Tracks.ElementAt(i);
                    sb.AppendLine($"{i + 1}. {track.Title} - [{track.Author}] [{track.Duration}]");
                    menu.AddOption((i + 1).ToString(), track.Uri?.ToString(), $"{track.Uri?.ToString()}");
                }
                var component = new ComponentBuilder();
                component.WithSelectMenu(menu);
                sb.AppendLine("```");

                await FollowupAsync(sb.ToString(), components: component.Build()).ConfigureAwait(false);
            }
        }*/
    }
}
