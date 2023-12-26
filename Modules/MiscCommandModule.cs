using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using IrisBot.Enums;
using IrisBot.Translation;
using System.Text;

namespace IrisBot.Modules
{
    public class MiscCommandModule : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("shard", "Display shard information")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ShardAsync()
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle($"{await TranslationLoader.GetTranslationAsync("shard_info_for", lang)}");
            foreach (var shard in Context.Client.Shards)
            {
                eb.AddField($"{await TranslationLoader.GetTranslationAsync("shard", lang)}: {shard.ShardId}", $"{shard.Latency} ms\n" +
                    $"{shard.Guilds.Count} {await TranslationLoader.GetTranslationAsync("server", lang)}\n" +
                    $"{shard.Guilds.Sum(x => x.MemberCount)} {await TranslationLoader.GetTranslationAsync("member", lang)}", true);
            }
            eb.WithDescription($"{await TranslationLoader.GetTranslationAsync("average_ping", lang)}: {Context.Client.Shards.Average(x => x.Latency)} ms");
            eb.WithFooter($"{await TranslationLoader.GetTranslationAsync("current_shard", lang)}: {Context.Client.GetShardFor(Context.Guild).ShardId}");
            eb.WithColor(Color.Purple);
            await RespondAsync("", embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("info", "Display bot information")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task InfoAsync()
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithAuthor(Context.Client.CurrentUser);

            eb.AddField($"{await TranslationLoader.GetTranslationAsync("irisbot_info", lang)}",
                $"[Github](https://github.com/asteria121/IrisBot)\r\n" +
                $"Copyright (c) 2023 Asteria. All rights reserved.\r\n" +
                $"[MIT License](https://github.com/asteria121/IrisBot/blob/master/LICENSE.txt)\r\n" +
                $"[{await TranslationLoader.GetTranslationAsync("tos", lang)}](https://github.com/asteria121/IrisBot/blob/master/Terms%20of%20service), " +
                $"[{await TranslationLoader.GetTranslationAsync("privacy_policy", lang)}](https://github.com/asteria121/IrisBot/blob/master/Privacy%20Policy)");

            eb.AddField("Discord.NET",
                "[Github](https://github.com/discord-net/Discord.Net)\r\n" +
                "Copyright (c) 2015-2022 Discord.Net Contributors. All rights reserved.\r\n" +
                "[MIT License](https://github.com/discord-net/Discord.Net/blob/dev/LICENSE)");

            eb.AddField("HtmlAgilityPack",
                "[Github](https://github.com/zzzprojects/html-agility-pack)\r\n" +
                "Copyright (c) ZZZ Projects Inc. All rights reserved.\r\n" +
                "[MIT License](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)");

            eb.AddField("Lavalink.NET",
                "[Github](https://github.com/angelobreuer/Lavalink4NET)\r\n" +
                "Copyright (c) 2019-2021 Angelo Breuer. All rights reserved.\r\n" +
                "[MIT License](https://github.com/angelobreuer/Lavalink4NET/blob/dev/LICENSE)");

            eb.AddField("Newtonsoft.Json",
                "[Github](https://github.com/JamesNK/Newtonsoft.Json)\r\n" +
                "Copyright (c) 2007 James Newton-King. All rights reserved.\r\n" +
                "[MIT License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)");

            eb.AddField("Data based on NEXON Open API",
                "이 프로그램은 [NEXON Open API](https://openapi.nexon.com)에서 데이터를 제공받습니다.\r\n" +
                "© NEXON Korea Corporation All Rights Reserved.");

            eb.WithFooter("Coded with C# (.NET Core 6.0)");
            eb.WithColor(Color.Purple);
            await RespondAsync("", embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("help", "help about command")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task HelpAsync()
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithAuthor(Context.Client.CurrentUser);
            eb.WithDescription($"{await TranslationLoader.GetTranslationAsync("bot_description", lang)}\r\n" +
                $"[Github](https://github.com/asteria121/IrisBot), " +
                $"[{await TranslationLoader.GetTranslationAsync("invitation_link", lang)}](https://discord.com/api/oauth2/authorize?client_id=930387137436721172&permissions=551940057088&scope=bot), " +
                $"[{await TranslationLoader.GetTranslationAsync("tos", lang)}](https://github.com/asteria121/IrisBot/blob/master/Terms%20of%20service), " +
                $"[{await TranslationLoader.GetTranslationAsync("privacy_policy", lang)}](https://github.com/asteria121/IrisBot/blob/master/Privacy%20Policy)\r\n" +
                $"파라미터의 <>는 빼고 입력하시기 바랍니다.");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_join", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_music", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_pause", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_resume", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_seek", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_volume", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_leave", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_searchmode", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_musictop", lang));
            eb.AddField(await TranslationLoader.GetTranslationAsync("help_music_header", lang), sb.ToString());
            sb.Clear();

            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_list", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_skip", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_shuffle", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_remove", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_mremove", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_clear", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_loop", lang));
            eb.AddField(await TranslationLoader.GetTranslationAsync("help_queue_header", lang), sb.ToString());
            sb.Clear();

            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_shard", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_info", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_language", lang));
            eb.AddField(await TranslationLoader.GetTranslationAsync("help_misc_header", lang), sb.ToString());
            sb.Clear();

            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_playlist_add", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_playlist_list", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_playlist_remove", lang));
            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_playlist_load", lang));
            eb.AddField(await TranslationLoader.GetTranslationAsync("help_playlist_header", lang), sb.ToString());
            sb.Clear();

            sb.AppendLine(await TranslationLoader.GetTranslationAsync("help_tmpchannel", lang));
            sb.AppendLine("`/wrrr` : WRRR");
            eb.AddField(await TranslationLoader.GetTranslationAsync("help_misc", lang), sb.ToString());
            sb.Clear();

            sb.AppendLine("`/전수조사` : 해당 캐릭터의 모든 월드 내 유니온 정보를 확인합니다.");
            sb.AppendLine("`/본캐` : 해당 캐릭터의 본캐릭터 정보를 출력합니다.");
            sb.AppendLine("`/신용점수` : 해당 캐릭터의 정보를 바탕으로 신용점수를 파악합니다.");
            eb.AddField("메이플스토리 명령어", sb.ToString());

            eb.WithColor(Color.Purple);
            await RespondAsync("", embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("tmp", "Make temporary voice channel")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task MakeTempChannelAsync(string name)
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            /*
            if (userLimit < 0 || userLimit > 99)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("create_tmp_channel_value_error", lang));
                return;
            }*/

            int bitrate = 96000;
            switch (Context.Guild.PremiumTier)
            {
                case PremiumTier.Tier1:
                    bitrate = 128000;
                    break;
                case PremiumTier.Tier2:
                    bitrate = 256000;
                    break;
                case PremiumTier.Tier3:
                    bitrate = 384000;
                    break;
            }

            var denyOverrides = new OverwritePermissions(viewChannel: PermValue.Deny);
            var allowOverrides = new OverwritePermissions(viewChannel: PermValue.Allow);

            List<Overwrite> list = new List<Overwrite>();
            foreach (var role in Context.Guild.Roles)
            {
                // 각 역할별로 Allow 권한을 준다.
                Overwrite overwrite = new Overwrite(role.Id, PermissionTarget.Role, allowOverrides);
                list.Add(overwrite);
            }
            // 마지막에 everyone 역할에 Deny 권한을 준다.
            list.Add(new Overwrite(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, denyOverrides));

            // Optional<IEnumerable<T>>로 변환
            IEnumerable<Overwrite> list2 = list;
            Optional<IEnumerable<Overwrite>> perm = new Optional<IEnumerable<Overwrite>>(list2);

            RestVoiceChannel channel = await Context.Guild.CreateVoiceChannelAsync(name, x =>
            {
                x.Bitrate = bitrate;
                x.PermissionOverwrites = perm;
            });

            await RespondAsync(TranslationLoader.GetTranslationAsync("create_tmp_channel", lang).GetAwaiter().GetResult().Replace("[NAME]", name));
            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000);
                    SocketVoiceChannel tmp = Context.Guild.GetVoiceChannel(channel.Id);
                    if (tmp == null)
                    {
                        await Context.Channel.SendMessageAsync(TranslationLoader.GetTranslationAsync("tmp_channel_admin_removed", lang).GetAwaiter().GetResult().Replace("[NAME]", name));
                        break;
                    }
                    else if (tmp.ConnectedUsers.Count == 0)
                    {
                        await Task.Delay(30000);
                        tmp = Context.Guild.GetVoiceChannel(channel.Id);
                        if (tmp == null)
                        {
                            await Context.Channel.SendMessageAsync(TranslationLoader.GetTranslationAsync("tmp_channel_admin_removed", lang).GetAwaiter().GetResult().Replace("[NAME]", name));
                            break;
                        }
                        else if (tmp.ConnectedUsers.Count == 0)
                        {
                            await channel.DeleteAsync();
                            await Context.Channel.SendMessageAsync(TranslationLoader.GetTranslationAsync("tmp_channel_expire", lang).GetAwaiter().GetResult().Replace("[NAME]", name));
                            break;
                        }
                    }
                }
            });
        }

        [SlashCommand("wrrr", "WRRR zz")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task WRRR()
        {
            await RespondWithFileAsync(Path.Combine(Program.ResourceDirectory, "WRRR.png"));
        }
    }
}
