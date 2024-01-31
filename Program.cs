using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IrisBot.Database;
using IrisBot.Modules;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;
using Lavalink4NET.InactivityTracking.Extensions;
using IrisBot.Player;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using System.Collections.Immutable;
using Lavalink4NET.InactivityTracking;

namespace IrisBot
{
    public class Program
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketConfig _socketConfig;
        private readonly DiscordShardedClient _client;

        public static string? TestGuildId { get; private set; }
        public static string? OpenApiKey { get; private set; }
        public static int PagelistCount { get; private set; }
        public static int MaxPlaylistCount { get; private set; }
        public static int MaxQueueCount { get; private set; }
        public static string Token { get; private set; }
        private static int ShardsCount = 1;
        private static int AutoDisconnectDelay = 600;
        private static string RestUri = "";
        private static string WebSocketUri = "";
        private static string Password = "";
        public static string BotMessage = "";
        public static string PlaylistDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Playlist"); }
        }
        public static string TranslationsDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translation"); }
        }
        public static string ResourceDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"); }
        }

        static void Main(string[] args)
        {
            LoadSettingsAsync().GetAwaiter().GetResult();
            GuildSettings.InitializeAsync().GetAwaiter().GetResult();

            var builder = new HostApplicationBuilder();
            builder.Services.AddSingleton<DiscordShardedClient>();
            builder.Services.AddSingleton(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged ^ GatewayIntents.GuildInvites ^ GatewayIntents.GuildScheduledEvents,
                ///AlwaysDownloadUsers = true, // TODO: PROBLEM
                LogLevel = LogSeverity.Verbose,
                TotalShards = ShardsCount,
                UseInteractionSnowflakeDate = false
            });
            builder.Services.AddSingleton<InteractionService>();
            builder.Services.AddHostedService<InteractionModule>();
            builder.Services.AddHostedService<MusicCommandModule>();

            // Lavalink
            builder.Services.AddLavalink();
            builder.Services.ConfigureLavalink(x =>
            {
                x.BaseAddress = new Uri(RestUri);
                //x.WebSocketUri = new Uri(WebSocketUri);
                x.Passphrase = Password;
            });
            builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // Inactivity Tracking
            builder.Services.AddInactivityTracking();
            builder.Services.AddSingleton<IInactivityTracker, IdleInactivityTracker>();
            builder.Services.Configure<IdleInactivityTrackerOptions>(options =>
            {
                options.Timeout = TimeSpan.FromSeconds(AutoDisconnectDelay);
            });

            builder.Build().Run();
        }

        private async static Task LoadSettingsAsync()
        {
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(jsonPath))
            {
                await CustomLog.PrintLog(LogSeverity.Error, "Bot", "appsettings.json is not exists.");
                Environment.Exit(1);
            }

            try
            {
                using (StreamReader file = File.OpenText(jsonPath))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    MaxQueueCount = 200;

                    JObject json = (JObject)JToken.ReadFrom(reader);
                    string? tmp = json["rest_uri"]?.ToString();
                    if (string.IsNullOrEmpty(tmp))
                    {
                        await CustomLog.PrintLog(LogSeverity.Error, "Bot", "\"rest_uri\" is empty on appsettings.json");
                        Environment.Exit(1);
                    }
                    else
                    {
                        RestUri = tmp;
                    }

                    tmp = json["websocket_uri"]?.ToString();
                    if (string.IsNullOrEmpty(tmp))
                    {
                        await CustomLog.PrintLog(LogSeverity.Error, "Bot", "\"websocket_uri\" is empty on appsettings.json");
                        Environment.Exit(1);
                    }
                    else
                    {
                        WebSocketUri = tmp;
                    }

                    tmp = json["password"]?.ToString();
                    if (string.IsNullOrEmpty(tmp))
                    {
                        await CustomLog.PrintLog(LogSeverity.Error, "Bot", "\"password\" is empty on appsettings.json");
                        Environment.Exit(1);
                    }
                    else
                    {
                        Password = tmp;
                    }

                    tmp = json["token"]?.ToString();
                    if (string.IsNullOrEmpty(tmp))
                    {
                        await CustomLog.PrintLog(LogSeverity.Error, "Bot", "\"token\" is empty on appsettings.json");
                        Environment.Exit(1);
                    }
                    else
                    {
                        Token = tmp;
                    }

                    tmp = TestGuildId = json["testguild_id"]?.ToString();
                    if (string.IsNullOrEmpty(TestGuildId) && IsDebug()) // testguild_id는 Debug에서만 필요함
                    {
                        await CustomLog.PrintLog(LogSeverity.Error, "Bot", "\"testguild_id\" is empty on appsettings.json");
                        Environment.Exit(1);
                    }
                    else
                    {
                        TestGuildId = tmp;
                    }

                    tmp = json["openapikey"]?.ToString();
                    if (string.IsNullOrEmpty(tmp))
                    {
                        await CustomLog.PrintLog(LogSeverity.Error, "Bot", "\"openapikey\" is empty on appsettings.json");
                        Environment.Exit(1);
                    }
                    else
                    {
                        OpenApiKey = tmp;
                    }

                    tmp = json["bot_message"]?.ToString();
                    if (string.IsNullOrEmpty(tmp))
                    {
                        BotMessage = "";
                    }
                    else
                    {
                        BotMessage = tmp;
                    }
                    
                    bool pagelistResult = int.TryParse(json["pagelist_count"]?.ToString(), out int pagelistCount);
                    if (pagelistResult)
                        PagelistCount = pagelistCount;
                    else
                    {
                        await CustomLog.PrintLog(LogSeverity.Warning, "Bot", "\"pagelist_count\" is empty on appsettings.json.\r\nAutomatically set to default value 10.");
                        PagelistCount = 10;
                    }


                    if (int.TryParse(json["max_playlist_count"]?.ToString(), out int playlistCount))
                    {
                        MaxPlaylistCount = playlistCount;
                    }
                    else
                    {
                        await CustomLog.PrintLog(LogSeverity.Warning, "Bot", "\"max_playlist_count\" is empty on appsettings.json.\r\nAutomatically set to default value 10.");
                        MaxPlaylistCount = 10;
                    }


                    if (int.TryParse(json["shards_count"]?.ToString(), out int shardsCount))
                    {
                        ShardsCount = shardsCount;
                    }
                    else
                    {
                        await CustomLog.PrintLog(LogSeverity.Warning, "Bot", "\"shards_count\" is empty on appsettings.json.\r\nAutomatically set to default value 1.");
                        ShardsCount = 1;
                    }

                    if (int.TryParse(json["auto_disconnect_delay"]?.ToString(), out int autoDisconnectDelay))
                    {
                        AutoDisconnectDelay = autoDisconnectDelay;
                    }
                    else
                    {
                        await CustomLog.PrintLog(LogSeverity.Warning, "Bot", "\"auto_disconnect_delay\" is empty on appsettings.json.\r\nAutomatically set to default value 600s.");
                        AutoDisconnectDelay = 600;
                    }
                }
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}
