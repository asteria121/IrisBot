using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IrisBot.Database;
using IrisBot.Enums;
using IrisBot.Player;
using IrisBot.Translation;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace IrisBot.Modules
{
    public class InteractionModule : IHostedService
    {
        private readonly DiscordShardedClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private IAudioService _audioService;
        public static int ReadyShards = 0;

        public InteractionModule(DiscordShardedClient client, InteractionService interactionService, IServiceProvider serviceProvider, IAudioService audioService)
        {
            _client = client;
            _handler = interactionService;
            _services = serviceProvider;
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _handler.Log += LogAsync;
            _client.ShardReady += ShardReady;
            _client.Log += Log2Async;
            
            _client.InteractionCreated += HandleInteraction;
            _client.SelectMenuExecuted += SelectMenuHandler;
            _client.ReactionAdded += ReactionAdded;
            _client.SlashCommandExecuted += SlashCommandExecuted;
            _client.MessageCommandExecuted += MessageCommandExecuted;
            _client.JoinedGuild += JoinGuildAsync;
            _client.LeftGuild += LeftGuildAsync;

            await _client.LoginAsync(TokenType.Bot, Program.Token);
            await _client.SetGameAsync(Program.BotMessage); // appsettings.json에서 현재 상태 메세지를 수정할 수 있음.
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _client.ShardReady -= ShardReady;
            _handler.Log -= LogAsync;
            _client.InteractionCreated -= HandleInteraction;
            _client.SelectMenuExecuted -= SelectMenuHandler;
            _client.ReactionAdded -= ReactionAdded;
            _client.SlashCommandExecuted -= SlashCommandExecuted;
            _client.MessageCommandExecuted -= MessageCommandExecuted;
            _client.JoinedGuild -= JoinGuildAsync;
            _client.LeftGuild -= LeftGuildAsync;

            await _client.StopAsync().ConfigureAwait(false);
        }

        private async Task JoinGuildAsync(SocketGuild guild)
        {
            if (GuildSettings.GetGuildsList().Find(x => x.GuildId == guild.Id) == null)
                await GuildSettings.AddNewGuildAsync(new GuildSettings(guild.Id, 0.5f)).ConfigureAwait(false); // 데이터베이스 및 서버 객체 추가
        }

        private async Task LeftGuildAsync(SocketGuild guild)
        {
            await GuildSettings.RemoveGuildDataAsync(guild.Id).ConfigureAwait(false); // 데이터베이스 및 서버 객체 삭제
            await Playlist.ClearPlaylistAsync(guild.Id).ConfigureAwait(false); // 플레이리스트 전체 삭제
        }

        public async Task ShardReady(DiscordSocketClient client)
        {
            ReadyShards++; // 샤드가 전부 연결되었을 때 커맨드를 등록해야한다.
            if (ReadyShards == _client.Shards.Count)
            {
                if (Program.IsDebug())
                {
                    await CustomLog.PrintLog(LogSeverity.Warning, "Bot",
                        $"Bot is running on Debug build. Commands will be registered only on specified guild id. ({Program.TestGuildId})");

                    await Task.Run(async () => await _handler.RegisterCommandsToGuildAsync(Convert.ToUInt64(Program.TestGuildId), true).ConfigureAwait(false)).ConfigureAwait(false);
                }
                else
                {
                    await CustomLog.PrintLog(LogSeverity.Info, "Bot", "Bot is running on Release build.");
                    await Task.Run(async () => await _handler.RegisterCommandsGloballyAsync(true).ConfigureAwait(false)).ConfigureAwait(false);
                }

                foreach (var guild in client.Guilds)
                {
                    if (GuildSettings.GetGuildsList().Find(x => x.GuildId == guild.Id) == null)
                    {
                        await GuildSettings.AddNewGuildAsync(new GuildSettings(guild.Id, 0.5f)).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task Log2Async(LogMessage log)
        {
            if (log.Exception == null)
                await CustomLog.PrintLog(log.Severity, log.Source, log.Message).ConfigureAwait(false);
            else
                await CustomLog.ExceptionHandler(log.Exception).ConfigureAwait(false);
        }

        private async Task SlashCommandExecuted(SocketSlashCommand cmd)
        {
            string param = "";
            foreach (var par in cmd.Data.Options)
                param += $" {par.Value}";
            
            await CustomLog.PrintLog(LogSeverity.Info, "Interaction",
                $"Slash Command executed (/{cmd.CommandName}{param}) (Guild: {cmd.GuildId}, Channel: {cmd.Channel.Name}, User: {cmd.User.Username})").ConfigureAwait(false);
        }

        private async Task MessageCommandExecuted(SocketMessageCommand cmd)
        {
            await CustomLog.PrintLog(LogSeverity.Info, "Interaction",
                $"Message Command executed {cmd.CommandName} (Guild: {cmd.GuildId}, Channel: {cmd.Channel.Name}, User: {cmd.User.Username})").ConfigureAwait(false);
        }

        static ValueTask<IrisPlayer> CreatePlayerAsync(IPlayerProperties<IrisPlayer, IrisPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new IrisPlayer(properties));
        }

        private async ValueTask<IrisPlayer?> GetPlayerAsync(SocketMessageComponent arg, bool connectToVoiceChannel = true, ImmutableArray<IPlayerPrecondition> precondition = default)
        {
            var channelBehavior = connectToVoiceChannel ? PlayerChannelBehavior.Join: PlayerChannelBehavior.None;
            var retrieveOptions = new PlayerRetrieveOptions(channelBehavior, MemberVoiceStateBehavior.RequireSame, precondition);

            var guildUser = (SocketGuildUser)arg.User;
            IOptions<IrisPlayerOptions> irisPlayerOptions = Options.Create(new IrisPlayerOptions(arg.Channel));
            
            if (arg.GuildId == null)
            {
                await arg.UpdateAsync(x =>
                {
                    x.Content = "🚫 ERROR: Cannot reference discord server ID.";
                    x.Components = null;
                    x.Embeds = null;
                }).ConfigureAwait(false);
                return null;
            }

            Translations lang = await TranslationLoader.FindGuildTranslationAsync((ulong)arg.GuildId).ConfigureAwait(false);

            // 플레이어가 연결되지 않은 상태에서 JoinAsync, RetrieveAsync(내부적으로 JoinAsync 호출)는 게이트웨이 스레드 데드락을 유발함.
            var results = _audioService.Players.TryGetPlayer((ulong)arg.GuildId, out IrisPlayer? ps);
            if (!results)
            {
                string errMsg = await TranslationLoader.GetTranslationAsync("expired_menu", lang);
                await arg.UpdateAsync(x =>
                {
                    x.Content = errMsg;
                    x.Components = null;
                });
                return null;
            }
            var result = await _audioService.Players.RetrieveAsync<IrisPlayer, IrisPlayerOptions>((ulong)arg.GuildId, guildUser.VoiceChannel.Id, CreatePlayerAsync, irisPlayerOptions, retrieveOptions).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => await TranslationLoader.GetTranslationAsync("should_joined", lang),
                    PlayerRetrieveStatus.BotNotConnected => await TranslationLoader.GetTranslationAsync("should_joined", lang),
                    PlayerRetrieveStatus.VoiceChannelMismatch => await TranslationLoader.GetTranslationAsync("different_channel_warning", lang),

                    PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Playing => await TranslationLoader.GetTranslationAsync("nothing_playing", lang),
                    PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPlaying => await TranslationLoader.GetTranslationAsync("player_already_running", lang),
                    PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPaused => await TranslationLoader.GetTranslationAsync("already_paused", lang),
                    PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Paused => await TranslationLoader.GetTranslationAsync("already_resumed", lang),
                    PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.QueueNotEmpty => await TranslationLoader.GetTranslationAsync("empty_queue", lang),

                    PlayerRetrieveStatus.PreconditionFailed when result.Player != null && result.Player.Queue.Count >= Program.MaxQueueCount => await TranslationLoader.GetTranslationAsync("maximum_queue", lang),
                    PlayerRetrieveStatus.PreconditionFailed when result.Player != null && result.Player.CurrentTrack == null => await TranslationLoader.GetTranslationAsync("nothing_playing", lang),

                    _ => await TranslationLoader.GetTranslationAsync("player_unknown_error", lang),
                };

                await arg.UpdateAsync(x =>
                {
                    x.Content = errorMessage;
                    x.Components = null;
                    x.Embeds = null;
                }).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction reaction)
        {

            var user = reaction.User.Value as SocketGuildUser;
            if (user == null)
                return;
            
            ulong? roleMessageId = GuildSettings.FindRoleMessageId(user.Guild.Id);
            List<string>? roleEmojiIds = GuildSettings.FindRoleEmojiIds(user.Guild.Id);
            
            var target = reaction.Emote as Emote;
            if (roleMessageId == reaction.MessageId && roleEmojiIds?.Count > 0)
            {
                foreach (string roleEmojiId in roleEmojiIds)
                {
                    if (string.IsNullOrEmpty(roleEmojiId)) continue;

                    string[] tmp = roleEmojiId.Split('|'); // TMP[0] = ROLE, TMP[1] = EMOJI
                    if (tmp[1] == target?.Id.ToString())
                    {
                        SocketRole? role = user.Guild.GetRole(Convert.ToUInt64(tmp[0]));
                        if (role != null && !user.IsBot)
                        {
                            await user.AddRoleAsync(role);
                            IMessage msg = await reaction.Channel.GetMessageAsync(reaction.MessageId);
                            if (msg != null) await msg.RemoveReactionAsync(target, user);
                        }
                    }
                }
            }
        }

        private async Task LogAsync(LogMessage log)
        {
            if (log.Exception == null)
                await CustomLog.PrintLog(log.Severity, log.Source, log.Message).ConfigureAwait(false);
            else
                await CustomLog.ExceptionHandler(log.Exception).ConfigureAwait(false);
        }

        private async Task ListPageViewAsync(SocketMessageComponent arg)
        {
            SocketGuildUser user = (SocketGuildUser)arg.User;
            string text = string.Join(", ", arg.Data.Values);
            if (arg.GuildId == null)
            {
                await arg.UpdateAsync(x =>
                {
                    x.Content = "🚫 ERROR: Cannot reference discord server ID.";
                    x.Components = null;
                }).ConfigureAwait(false);
                
                return;
            }

            var player = await GetPlayerAsync(arg, connectToVoiceChannel: false).ConfigureAwait(false);
            if (player == null)
                return;

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(arg.GuildId.Value);
            StringBuilder sb = new StringBuilder();
            if (player.CurrentTrack == null)
            {
                sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("now_playing", lang)}: N/A```");
            }
            else
            {
                int bitrate = 0;
                var channel = user.Guild.GetVoiceChannel(player.VoiceChannelId);
                if (channel != null)
                    bitrate = channel.Bitrate / 1000;

                sb.AppendLine($"```{await TranslationLoader.GetTranslationAsync("now_playing", lang)}: {player.CurrentTrack.Title} - {player.CurrentTrack.Author} " +
                    $"[{player.Position?.RelativePosition.ToString(@"hh\:mm\:ss")}/{player.CurrentTrack.Duration.ToString(@"hh\:mm\:ss")}] " +
                    $"[{bitrate} Kbps]```");
                // Now playing: Song name - [AuthorName] [00:00:00/01:10:23] [96 Kbps]
            }

            ComponentBuilder? component = null;
            if (player.Queue == null || player.Queue.Count == 0)
            {
                sb.AppendLine("```Nothing in queue.```");
            }
            else
            {
                int startPage = (Convert.ToInt32(text) - 1) * Program.PagelistCount;
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
                for (int i = startPage; i < startPage + Program.PagelistCount && i < player.Queue.Count; i++)
                {
                    LavalinkTrack? track = player.Queue.ElementAt(i).Track;
                    sb.AppendLine($"{i + 1}. {track?.Title} - [{track?.Author}] [{track?.Duration.ToString(@"hh\:mm\:ss")}]");
                    // 1. Song name - [AuthorName] [01:10:23]
                }
                sb.AppendLine($"\r\nPage {Convert.ToInt32(text)}/{pageCount}```");
            }

            await arg.UpdateAsync(x =>
            {
                x.Content = sb.ToString();
                x.Components = component?.Build() ?? null;
            }).ConfigureAwait(false);
        }

        private async Task MusicSelectAsync(SocketMessageComponent arg)
        {
            var selectedId = string.Join(", ", arg.Data.Values);
            if (arg.GuildId == null)
                return;

            var preconditions = ImmutableArray.Create(item: PlayerPrecondition.Create<IrisPlayer>(precondition: x => x.Queue.Count < Program.MaxQueueCount));
            var player = await GetPlayerAsync(arg, precondition: preconditions).ConfigureAwait(false);
            if (player == null)
                return;
            
            Translations lang = await TranslationLoader.FindGuildTranslationAsync((ulong)arg.GuildId).ConfigureAwait(false);
            SocketGuildUser user = (SocketGuildUser)arg.User;
            GuildSettings? data = GuildSettings.GetGuildsList().Find(x => x.GuildId == (ulong)arg.GuildId);

            // 설정된 볼륨으로 초기화
            if (data == null)
                await player.SetVolumeAsync(0.5f).ConfigureAwait(false);
            else
                await player.SetVolumeAsync(data.PlayerVolume).ConfigureAwait(false);

            TrackSearchMode mode = GuildSettings.FindGuildSearchMode(arg.GuildId.Value);
            TrackLoadResult searchResult;
            if (arg.Data.CustomId == "music_select" || arg.Data.CustomId == "music_select_top") // YouTube 검색 모드인경우
                searchResult = await _audioService.Tracks.LoadTracksAsync(selectedId, TrackSearchMode.YouTube).ConfigureAwait(false);
            else // SoundCloud 검색 모드인 경우
                searchResult = await _audioService.Tracks.LoadTracksAsync(selectedId, TrackSearchMode.SoundCloud).ConfigureAwait(false);
            
            if (!searchResult.IsSuccess)
            {
                await arg.UpdateAsync(x =>
                {
                    x.Content = "Failed to fetch track information.";
                    x.Components = null;
                    x.Embeds = null;
                }).ConfigureAwait(false);
                return;
            }

            IrisTrack track = new IrisTrack(new TrackReference(searchResult.Track));
            track.Requester = arg.User.Username;

            if (arg.Data.CustomId.Contains("_top")) // 우선순위 예약 명령일 경우
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle(await TranslationLoader.GetTranslationAsync("top_priority_queue", lang))
                    .WithDescription($"[{track.Reference.Track?.Title}]({track.Reference.Track?.Uri})" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("author", lang)} : `{track.Reference.Track?.Author}`" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("duration", lang)} : `{track.Reference.Track?.Duration.ToString(@"hh\:mm\:ss")}`")
                    .WithColor(Color.Purple);

                // TODO: PLAYTOP
                //await player.PlayTopAsync(myTrack);
                await arg.UpdateAsync(x =>
                {
                    x.Embed = eb.Build();
                    x.Content = "";
                    x.Components = null;
                }).ConfigureAwait(false);
            }
            else // 우선순위 예약 명령이 아닌 경우
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithTitle(await TranslationLoader.GetTranslationAsync("added_queue", lang))
                    .WithDescription($"[{track.Reference.Track?.Title}]({track.Reference.Track?.Uri})" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("author", lang)} : `{track.Reference.Track?.Author}`" +
                        $"\r\n{await TranslationLoader.GetTranslationAsync("duration", lang)} : `{track.Reference.Track?.Duration.ToString(@"hh\:mm\:ss")}`")
                    .WithColor(Color.Purple);
                
                await player.PlayAsync(track).ConfigureAwait(false);
                await arg.UpdateAsync(x =>
                {
                    x.Embed = eb.Build();
                    x.Content = "";
                    x.Components = null;
                }).ConfigureAwait(false);
            }
        }

        private async Task SelectMenuHandler(SocketMessageComponent arg)
        {
            if (arg.Data.CustomId == "music_select" || arg.Data.CustomId == "music_select_top"
                || arg.Data.CustomId == "music_select_soundcloud" || arg.Data.CustomId == "music_select_soundcloud_top")
            {
                await MusicSelectAsync(arg).ConfigureAwait(false);
            }
            else if (arg.Data.CustomId == "list_pageview")
            {
                await ListPageViewAsync(arg).ConfigureAwait(false);
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                if (interaction.IsDMInteraction)
                {
                    await interaction.RespondAsync("Slash commands on DM is not supported.").ConfigureAwait(false);
                    return;
                }

                var context = new ShardedInteractionContext(_client, interaction);

                var result = await _handler.ExecuteCommandAsync(context, _services).ConfigureAwait(false);
                await CustomLog.PrintLog(LogSeverity.Info, "Interaction",
                    $"Command executed (Guild: {interaction.GuildId}, Channel: {interaction.Channel.Name}, User: {interaction.User.Username})").ConfigureAwait(false);

                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        default:
                            break;
                    }
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex).ConfigureAwait(false);
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync().ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
    }
}
