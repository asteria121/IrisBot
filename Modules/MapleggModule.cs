using Discord;
using Discord.Interactions;
using IrisBot.NexonAPI;
using System.Text;

namespace IrisBot.Modules
{
    public class MapleggModule : InteractionModuleBase<ShardedInteractionContext>
    {
        [SlashCommand("전수조사", "모든 월드의 본캐릭터를 검색합니다")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task FindAllCharacterAsync(string nickname)
        {
            EmbedBuilder eb = new EmbedBuilder();

            try
            {
                UserInfo user = await UserInfo.CreateAsync(nickname, true);
                if (user.UnionRankings != null && user.UnionRankings.Count() > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < user.UnionRankings.Count(); i++)
                        sb.AppendLine($"- {user.UnionRankings[i].WorldName}: {user.UnionRankings[i].CharacterName} / Union Lv. {user.UnionRankings[i].UnionLevel}");

                    eb.AddField("ℹ️ 전체 월드 본캐릭터 정보", sb.ToString());
                    eb.WithColor(Color.Purple);
                    await RespondAsync("", embed: eb.Build(), ephemeral: true);
                }
            }
            catch (NexonAPIExceptions ex)
            {
                await HandleNexonAPIException(nickname, ex);
            }
        }

        [SlashCommand("본캐", "해당 캐릭터가 본캐릭터인지 확인합니다")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task MainCharacterAsync(string nickname)
        {
            EmbedBuilder eb = new EmbedBuilder();

            try
            {
                UserInfo user = await UserInfo.CreateAsync(nickname, true);

                if (!string.Equals(user.UnionMainCharacter, nickname))
                {
                    if (!string.IsNullOrEmpty(user.UnionMainCharacter))
                    {
                        eb.WithDescription($"\"{nickname}\"님은 본캐릭터가 아닙니다.");
                        user = await UserInfo.CreateAsync(user.UnionMainCharacter, false);
                    }
                    else
                    {
                        eb.WithTitle($"본캐릭터 조회 결과");
                        await RespondAsync("ℹ️ 본캐릭터: 오류");
                        return;
                    }
                }
               else
                {
                    eb.WithDescription($"\"{nickname}\"님은 본캐릭터가 맞습니다.");
                }

                StringBuilder sb = BuildUserInfo(user);
                eb.AddField("ℹ️ 캐릭터 정보", sb.ToString());
                sb = BuildGuildInfo(user);
                eb.AddField("ℹ️ 길드 정보", sb.ToString());
                eb.WithColor(Color.Purple);
                await RespondAsync("", embed: eb.Build(), ephemeral: true);

            }
            catch (NexonAPIExceptions ex)
            {
                await HandleNexonAPIException(nickname, ex);
            }
        }

        [SlashCommand("신용점수", "NEXON Open API 및 MAPLE.GG 데이터를 바탕으로 신용점수를 매깁니다")]
        [RequireBotPermission(GuildPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.SendMessages)]
        public async Task MapleggSearchAsync(string nickname)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle($"신용점수 조회 결과");

            try
            {
                UserInfo user = await UserInfo.CreateAsync(nickname, true);
                StringBuilder sb = BuildUserInfo(user);

                if (!string.Equals(user.UnionMainCharacter, user.NickName))
                {
                    if (!string.IsNullOrEmpty(user.UnionMainCharacter))
                        sb.AppendLine($"⚠️ 본캐릭터 닉네임: {user.UnionMainCharacter}");
                    else
                        sb.AppendLine("⚠️ 본캐릭터 닉네임: 오류");
                }
                    
                eb.AddField("ℹ️ 캐릭터 정보", sb.ToString());

                sb = BuildGuildInfo(user);
                eb.AddField("ℹ️ 길드 정보", sb.ToString());

                if (user.Score <= 30)
                {
                    eb.AddField("신용 점수", $"⚠️ {user.Score}점 - 신용 점수가 매우 낮습니다. 거래에 신중을 가해주세요.");
                }
                else if (user.Score <= 60)
                {
                    eb.AddField("신용 점수", $"⚠️ {user.Score}점 - 신용 점수가 조금 낮습니다. 거래에 신중을 가해주세요.");
                }
                else
                {
                    eb.AddField("신용 점수", $"{user.Score} / 100점");
                }

                if (user.Message.Length > 0)
                    eb.AddField("점수에 반영된 지표", user.Message);

                eb.AddField("거래 전 주의사항 및 사기꾼 패턴",
                    "중요) 반드시 거래대상의 길드 기여도를 확인하시기 바랍니다. 개발자 권장 수치는 기여도 50만 이상입니다.\r\n" +
                    "1. 신용점수가 낮은것은 단순히 신용도가 낮음을 의미합니다. 신용점수는 해당 캐릭터가 사기꾼이라는 물증으로는 사용할 수 없습니다.\r\n" +
                    "2. 신용점수는 어떠한 방법으로도 해당 캐릭터를 보증할 수 없습니다. 신용 점수는 반드시 참고용으로만 사용되야합니다.\r\n" +
                    "3. 신용점수와 더불어 기여도 혹은 서버 내 유명도 등을 확인 후 주관에 따른 종합적인 판단이 필요합니다.\r\n" +
                    "4. 모든 거래는 잠재적인 사기의 위험성을 동반합니다.\r\n" +
                    "5. 최근 사기꾼들은 낮은 가격의 통구매 할인 또는 각종 인증으로 피해자를 기만합니다.\r\n" +
                    "6. 판매자의 메소가 계속 줄지 않은채 확성기를 사용한다면 사기로 의심할 수 있습니다.\r\n" +
                    "7. 현금거래 및 계정양도는 메이플스토리 운영정책을 위배합니다. " +
                    "현금거래 및 계정양도 후 피해 발생시 어떠한 공식 복구 서비스도 받으실 수 없습니다. 또한 이로 인한 불이익은 본인 책임입니다.");

                eb.AddField("왜 기여도가 중요한가요?",
                    "기여도는 하루에 5000까지 채울 수 있습니다. 기여도가 높다는 것은 " +
                    "길드에 가입 후 오랜 시간이 지난것을 의미합니다." +
                    "사기꾼은 한 길드에 오래 있기 힘들기 때문에 기여도는 신용점수만큼 혹은 그 이상의 가치가 있습니다." +
                    "따라서 50만 이상의 기여도를 가진 대상과의 거래를 권장합니다.");

                eb.WithColor(Color.Purple);
                await RespondAsync("", embed: eb.Build(), ephemeral: true);
            }
            catch (NexonAPIExceptions ex)
            {
                if (ex.ErrorCode == NexonAPIErrorCode.OPENAPI00004)
                    await RespondAsync($"⚠️ {nickname} 캐릭터는 존재하지 않거나 당일에 닉네임이 변경 또는 생성된 아이디입니다.\r\n인게임에 존재한다면 해당 캐릭터는 사기꾼일 확률이 매우 높습니다.\r\n" +
                        $"2023년 12월 21일 이전 접속 기록이 없는 캐릭터는 조회할 수 없습니다.", ephemeral: true);
                else
                    await RespondAsync($"🚫 데이터를 가져오는 중 오류가 발생했습니다.\r\n{ex.ErrorCode}: {ex.Message}", ephemeral: true);

                await CustomLog.ExceptionHandler(ex);
            }
        }

        private async Task HandleNexonAPIException(string nickname, NexonAPIExceptions ex)
        {
            if (ex.ErrorCode == NexonAPIErrorCode.OPENAPI00004)
                await RespondAsync($"⚠️ {nickname} 캐릭터는 존재하지 않거나 당일에 닉네임이 변경 또는 생성된 아이디입니다.\r\n" +
                    $"2023년 12월 21일 이전 접속 기록이 없는 캐릭터는 조회할 수 없습니다.", ephemeral: true);
            else
                await RespondAsync($"🚫 데이터를 가져오는 중 오류가 발생했습니다.\r\n{ex.ErrorCode}: {ex.Message}", ephemeral: true);

            await CustomLog.ExceptionHandler(ex);
        }

        private StringBuilder BuildUserInfo(UserInfo user)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"- 월드: {user.World}");
            sb.AppendLine($"- 닉네임: [{user.NickName}](https://maple.gg/u/{user.NickName})");
            sb.AppendLine($"- 레벨: {user.Level}");
            sb.AppendLine($"- 직업: {user.Job}");
            sb.AppendLine($"- 유니온: {user.Union}");
            sb.AppendLine($"- 인기도: {user.Popularity}");
            if (user.DojangFloor == 0)
                sb.AppendLine("- 무릉도장: 기록 없음");
            else
                sb.AppendLine($"- 무릉도장: {user.DojangFloor}층");
            if (string.IsNullOrEmpty(user.CombatPower))
                sb.AppendLine("- 전투력: 기록 없음");
            else
                sb.AppendLine($"- 전투력: {ConvertNumber(user.CombatPower)}");

            return sb;
        }

        private StringBuilder BuildGuildInfo(UserInfo user)
        {
            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(user.Guild))
            {
                sb.AppendLine("- 길드명: 없음");
            }
            else
            {
                sb.AppendLine($"- 길드명: {user.Guild} / {user.GuildSize} 명");

                if (user.GuildFameRank == 0)
                    sb.AppendLine("- 명성치 월드 랭킹: 없음");
                else
                    sb.AppendLine($"- 명성치 월드 랭킹: {user.GuildFameRank} 위");
                if (user.GuildRaceRank == 0)
                    sb.AppendLine("- 플래그레이스 월드 랭킹: 없음");
                else
                    sb.AppendLine($"- 플래그레이스 월드 랭킹: {user.GuildRaceRank} 위");
                if (user.GuildPunchRank == 0)
                    sb.AppendLine("- 지하수로 월드 랭킹: 없음");
                else
                    sb.AppendLine($"- 지하수로 월드 랭킹: {user.GuildPunchRank} 위");
                if (user.GuildNoblessSP == 0)
                    sb.AppendLine("- 노블레스 스킬 포인트: 없음");
                else
                    sb.AppendLine($"- 노블레스 스킬 포인트: {user.GuildNoblessSP}");
            }

            return sb;
        }

        public string ConvertNumber(string number)
        {
            bool conversionResult = long.TryParse(number, out long convertedNumber);
            if (!conversionResult)
                return "파라미터 오류";

            string num = string.Format("{0:# #### #### #### #### ####}", convertedNumber).TrimStart().Replace(" ", ",");

            string[] unit = new string[] { "", "만", "억", "조", "경", "해" };
            string[] str = num.Split(',');
            string result = "";
            int cnt = 0;
            for (int i = str.Length; i > 0; i--)
            {
                if (Convert.ToInt64(str[i - 1]) != 0)
                {
                    result = Convert.ToInt64(str[i - 1]) + unit[cnt] + result;
                }
                cnt++;
            }
            return result;
        }
    }
}
