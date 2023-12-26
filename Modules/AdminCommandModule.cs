using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using IrisBot.Database;
using IrisBot.Enums;
using IrisBot.Translation;
using System.Text;

namespace IrisBot.Modules
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class AdminCommandModule : InteractionModuleBase<ShardedInteractionContext>
    {
        public async Task<GuildEmote?> IsExistsEmojiAsync(SocketGuild guild, string emojiId)
        {
            foreach (var em in await guild.GetEmotesAsync())
            {
                if (string.Equals(em.Id.ToString(), emojiId.ToString()))
                    return em;
            }

            return null;
        }

        [SlashCommand("addemoji", "Add emoji role")]
        public async Task AddEmojiAsync(string emoji, IRole role)
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            bool isCustomEmoji = Emote.TryParse(emoji, out Emote customEmoji);
            SocketRole? botRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Iris Player");
            if (role.Permissions.Administrator)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emoji_admin_reject", lang));
            }
            else if (botRole != null && role.Position > botRole.Position)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emoji_role_position_error", lang));
            }
            else if (isCustomEmoji)
            {
                await GuildSettings.UpdateRoleEmojiIdsAsync(Context.Guild.Id, role.Id, customEmoji.Id);
                await RespondAsync($"{await TranslationLoader.GetTranslationAsync("emoji_add_success", lang)}\r\n{customEmoji.ToString()} | {role.Name}");
            }
            else
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emoji_not_exists", lang));
            }
        }

        [SlashCommand("emojilist", "View list of emoji role")]
        public async Task ListEmoji()
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            List<string>? emojiList = GuildSettings.FindRoleEmojiIds(Context.Guild.Id);
            if (emojiList == null || emojiList.Count == 0)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emojirole_not_exists", lang));
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                foreach (var val in emojiList)
                {
                    if (string.IsNullOrEmpty(val)) continue;

                    string[] tmp = val.Split('|'); // TMP[0] = ROLE, TMP[1] = EMOJI
                    GuildEmote? emote = await IsExistsEmojiAsync(Context.Guild, tmp[1]);
                    var role = Context.Guild.GetRole(Convert.ToUInt64(tmp[0]));

                    sb.Append($"{i}. ");
                    if (emote == null)
                        sb.Append(await TranslationLoader.GetTranslationAsync("removed_emoji", lang));
                    else
                        sb.Append(emote.ToString());

                    sb.Append(" | ");
                    if (role == null)
                        sb.Append(await TranslationLoader.GetTranslationAsync("removed_role", lang) + "\r\n");
                    else
                        sb.Append(role.Name + "\r\n");
                    i++;
                }

                if (string.IsNullOrEmpty(sb.ToString()))
                    await RespondAsync(await TranslationLoader.GetTranslationAsync("emojirole_not_exists", lang));
                else
                    await RespondAsync(sb.ToString(), ephemeral: true);
            }
        }

        [SlashCommand("rmemoji", "Remove emoji role")]
        public async Task RemoveEmoji(int index)
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            List<string>? emojiList = GuildSettings.FindRoleEmojiIds(Context.Guild.Id);
            if (emojiList == null || emojiList.Count == 0)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emojirole_not_exists", lang));
            }
            else if (index >= emojiList.Count || index < 0)
            {
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emoji_correct_number", lang));
            }
            else
            {
                string[] tmp = emojiList.ElementAt(index).Split("|"); // TMP[0] = ROLE, TMP[1] = EMOJI
                await GuildSettings.RemoveEmojiAsync(Context.Guild.Id, Convert.ToUInt64(tmp[0]), Convert.ToUInt64(tmp[1]));
                await RespondAsync(await TranslationLoader.GetTranslationAsync("emoji_remove_success", lang));
            }
        }

        [MessageCommand("rolemessage")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task Role(IMessage message)
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            await GuildSettings.UpdateRoleMessageIdAsync(message.Id, Context.Guild.Id);

            List<string>? emojiList = GuildSettings.FindRoleEmojiIds(Context.Guild.Id);
            if (emojiList != null)
            {
                foreach (var val in emojiList)
                {
                    if (string.IsNullOrEmpty(val)) continue;

                    string[] tmp = val.Split('|'); // TMP[0] = ROLE, TMP[1] = EMOJI
                    GuildEmote? emote = await IsExistsEmojiAsync(Context.Guild, tmp[1]);

                    if (emote != null)
                        await message.AddReactionAsync(emote, null);
                }
            }

            await RespondAsync(await TranslationLoader.GetTranslationAsync("emoji_message_change_success", lang));
        }

        [SlashCommand("language", "Set bot language")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task Language(Translations language)
        {
            await GuildSettings.UpdateLanguageAsync(language, Context.Guild.Id);

            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            await RespondAsync(await TranslationLoader.GetTranslationAsync("language_change", lang));
        }

        [SlashCommand("tmpprivate", "Toggle private channel private.")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task ToggleTmpPrivate(bool isPrivate)
        {
            Translations lang = await TranslationLoader.FindGuildTranslationAsync(Context.Guild.Id);
            await GuildSettings.UpdateIsPrivateAsync(isPrivate, Context.Guild.Id);
            if (isPrivate)
                await RespondAsync(await TranslationLoader.GetTranslationAsync("tmp_channel_isprivate_true", lang));
            else
                await RespondAsync(await TranslationLoader.GetTranslationAsync("tmp_channel_isprivate_false", lang));
        }
    }
}
