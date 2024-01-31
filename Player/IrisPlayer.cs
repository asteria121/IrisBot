using Discord.Interactions;
using Discord.WebSocket;
using IrisBot.Enums;
using IrisBot.Translation;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

namespace IrisBot.Player
{
    internal sealed class IrisPlayer : QueuedLavalinkPlayer, IInactivityPlayerListener
    {
        // InactivityTrackingService 에서 안내 메세지를 보낼 채널 아이디를 보관하기 위해 CustomPlayer 클래스를 만들어 Channel 멤버변수 생성.
        // 기본 Player 클래스에는 해당 항목이 존재하지 않음.
        public readonly ISocketMessageChannel Channel;

        static ValueTask<IrisPlayer> CreatePlayerAsync(IPlayerProperties<IrisPlayer, IrisPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new IrisPlayer(properties));
        }

        public static async ValueTask<IrisPlayer?> GetPlayerAsync(ShardedInteractionContext ctx, IAudioService audioService,
            bool connectToVoiceChannel = false, bool requireSameChannel = true, ImmutableArray<IPlayerPrecondition> precondition = default)
        {
            var channelBehavior = connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None;
            var voiceStateBehavior = requireSameChannel ? MemberVoiceStateBehavior.RequireSame : MemberVoiceStateBehavior.AlwaysRequired;
            var retrieveOptions = new PlayerRetrieveOptions(channelBehavior, voiceStateBehavior, precondition);

            var guildUser = (SocketGuildUser)ctx.User;
            var irisPlayerOptions = Options.Create(new IrisPlayerOptions(ctx.Channel));
            var result = await audioService.Players.RetrieveAsync<IrisPlayer, IrisPlayerOptions>(ctx.Guild.Id, guildUser.VoiceChannel?.Id, CreatePlayerAsync, irisPlayerOptions, retrieveOptions).ConfigureAwait(false);
            
            if (!result.IsSuccess)
            {
                Translations lang = await TranslationLoader.FindGuildTranslationAsync(ctx.Guild.Id).ConfigureAwait(false);
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
                
                await ctx.Interaction.FollowupAsync(errorMessage).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }

        public IrisPlayer(IPlayerProperties<IrisPlayer, IrisPlayerOptions> properties) : base(properties)
        {
            Channel = properties.Options.Value.Channel;
        }

        public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            if (Channel is not null)
            {
                Translations lang = await TranslationLoader.FindGuildTranslationAsync(GuildId).ConfigureAwait(false);
                await Channel.SendMessageAsync(await TranslationLoader.GetTranslationAsync("inactivity_disconnect", lang)).ConfigureAwait(false);
            }
        }
        
#pragma warning disable CS1998 // 이 비동기 메서드에는 'await' 연산자가 없으며 메서드가 동시에 실행됩니다.
        public async ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            if (Channel is not null)
            {
                //await Channel.SendMessageAsync("Player is being tracked as active.").ConfigureAwait(false);
            }
        }

        public async ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
        {
            if (Channel is not null)
            {
                //await Channel.SendMessageAsync("Player is being tracked as inactive.").ConfigureAwait(false);
            }
        }
#pragma warning restore CS1998 // 이 비동기 메서드에는 'await' 연산자가 없으며 메서드가 동시에 실행됩니다.
    }
}
