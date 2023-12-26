using System.Text;
using HtmlAgilityPack;
using IrisBot.Interfaces;
using IrisBot.NexonAPI;
using IrisBot.NexonAPI.Responses;
using IrisBot.Enums;

namespace IrisBot
{
    public class ExpHistory
    {
        public DateTime? Date { get; set; }
        public int? Level { get; set; }
        public double? Exp { get; set; }
        public string? ExpScore { get; set; }

        public ExpHistory(string? date, string? level, string? exp, string? expScore)
        {
            if (long.TryParse(date, out long unixTime))
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
                DateTimeOffset timeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTime);
                timeOffset = TimeZoneInfo.ConvertTime(timeOffset, timeZoneInfo);
                Date = timeOffset.UtcDateTime;
            }
            else
            {
                Date = null;
            }

            if (int.TryParse(level, out int tmp))
                Level = tmp;
            else
                Level = null;

            if (double.TryParse(exp, out double tmp2))
                Exp = tmp2;
            else
                Exp = null;

            ExpScore = expScore;
        }
    }

    public class UserInfo : IUser
    {
        public string NickName { get; private set; }
        public string World { get; private set; }
        public long Level { get; private set; }
        public string Job { get; private set; }
        public long Popularity { get; private set; }
        public long DojangFloor { get; private set; }
        public long Union { get; private set; }
        public string UnionMainCharacter { get; private set; }
        public string CombatPower { get; private set; }
        public List<UnionRanking> UnionRankings { get; private set; }
        public List<ExpHistory>? ExpHistories { get; private set; }
        public string Guild { get; private set; }
        public long GuildFameRank { get; private set; }
        public long GuildRaceRank { get; private set; }
        public long GuildPunchRank { get; private set; }
        public long GuildSize { get; private set; }
        public long GuildNoblessSP { get; private set; }
        public int Score { get; private set; }
        public StringBuilder Message { get; private set; }

        public static async Task<UserInfo> CreateAsync(string nickname, bool calcScore)
        {
            CharacterOCID ocid = await NexonAPIManager.GetUserOcidAsync(nickname);

            if (string.IsNullOrEmpty(ocid.Ocid))
            {
                Exception ex = new Exception("캐릭터 Ocid가 Null 입니다.");
                throw ex;
            }

            CharacterBasic basic = await NexonAPIManager.GetCharacterBasicAsync(ocid.Ocid);
            CharacterPopularity popularity = await NexonAPIManager.GetCharacterPopularityAsync(ocid.Ocid);
            CharacterUnion union = await NexonAPIManager.GetCharacterUnionAsync(ocid.Ocid);
            UnionRankingBody unionRanking = await NexonAPIManager.GetCharacterUnionRankingAsync(basic.WorldName, ocid.Ocid);
            CharacterDojang dojang = await NexonAPIManager.GetCharacterDojangAsync(ocid.Ocid);
            CharacterStatBody stat = await NexonAPIManager.GetCharacterStatAsync(ocid.Ocid);

            // 캐릭터 기본 정보 입력
            UserInfo userInfo = new UserInfo();
            userInfo.NickName = nickname;
            // 월드명
            userInfo.World = basic.WorldName;
            // 직업
            userInfo.Job = basic.Class;
            // 레벨
            userInfo.Level = basic.Level;
            // 인기도
            userInfo.Popularity = popularity.Popularity;
            // 유니온 레벨
            userInfo.Union = union.UnionLevel;
            // 모든 월드 유니온
            userInfo.UnionRankings = unionRanking.Ranking;
            // 유니온 랭킹 (본캐릭터 정보 포함)
            userInfo.UnionMainCharacter = unionRanking.GetWorldMainCharacter(basic.WorldName, nickname);
            // 무릉도장
            userInfo.DojangFloor = dojang.BestFloor;
            // 전투력
            userInfo.CombatPower = stat.GetCombatPower();

            // 길드
            if (!string.IsNullOrEmpty(basic.GuildName))
            {
                GuildID guildId = await NexonAPIManager.GetGuildIdAsync(basic.GuildName, basic.WorldName);
                GuildInformation guildInformation = new GuildInformation();
                guildInformation.Basic = await NexonAPIManager.GetGuildInformationAsync(guildId.Id);
                guildInformation.FameRanking = await NexonAPIManager.GetGuildRankingAsync(basic.WorldName, GuildRankType.Fame, basic.GuildName);
                guildInformation.RaceRanking = await NexonAPIManager.GetGuildRankingAsync(basic.WorldName, GuildRankType.Race, basic.GuildName);
                guildInformation.PunchRanking = await NexonAPIManager.GetGuildRankingAsync(basic.WorldName, GuildRankType.Punch, basic.GuildName);

                // 길드 기본 정보 입력
                userInfo.Guild = basic.GuildName;
                userInfo.GuildSize = guildInformation.Basic.GuildMemberCount;

                // 명성치 랭킹
                if (guildInformation.FameRanking != null && guildInformation.FameRanking.Ranking.Count() > 0)
                    userInfo.GuildFameRank = guildInformation.FameRanking.Ranking[0].Ranking;
                else
                    userInfo.GuildFameRank = 0;

                // 플래그레이스 랭킹
                if (guildInformation.RaceRanking != null && guildInformation.RaceRanking.Ranking.Count() > 0)
                    userInfo.GuildRaceRank = guildInformation.RaceRanking.Ranking[0].Ranking;
                else
                    userInfo.GuildRaceRank = 0;

                // 지하수로 랭킹
                if (guildInformation.PunchRanking != null && guildInformation.PunchRanking.Ranking.Count() > 0)
                    userInfo.GuildPunchRank = guildInformation.PunchRanking.Ranking[0].Ranking;
                else
                    userInfo.GuildPunchRank = 0;

                // 노블레스 스킬 포인트
                userInfo.GuildNoblessSP = guildInformation.Basic.GetGuildNoblessSP();
            }
            else
            {
                userInfo.Guild = "없음";
            }

            if (calcScore)
            {
                // 최근 경험치 증감폭을 측정함 
                userInfo.ExpHistories = await AnalyzieExpHistoryAsync(nickname);

                // 최종 신용점수 계산
                userInfo.Score = userInfo.CalculateScore();
            }

            return userInfo;
        }

        public static async Task<List<ExpHistory>?> AnalyzieExpHistoryAsync(string nickname)
        {
            string url = $"https://maple.gg/u/{nickname}";
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = await web.LoadFromWebAsync(url);

            var scripts = htmlDoc.DocumentNode.Descendants("script");
            foreach (var script in scripts)
            {
                if (script.InnerHtml.Contains("characterExpLogs"))
                {
                    string textStart = "\\\"characterExpLogs\\\":[";
                    string textEnd = "],\\\"dojangLogs";
                    int startIndex = script.InnerHtml.IndexOf(textStart);
                    int endIndex = script.InnerHtml.IndexOf(textEnd);
                    var histories = script.InnerHtml.Substring(startIndex + textStart.Length, endIndex - startIndex - textStart.Length);

                    List<ExpHistory> list = new List<ExpHistory>();
                    foreach (string history in histories.Split("],["))
                    {
                        string tmp = history.Replace("[", "");
                        tmp = tmp.Replace("]", "");
                        string[] innerHistories = tmp.Split(",");
                        list.Add(new ExpHistory(innerHistories[0], innerHistories[1], innerHistories[2], innerHistories[3]));
                    }
                    return list;
                }
            }
            return null;
        }

        public int CalculateScore()
        {
            Message = new StringBuilder();
            int totalScore = 0;

            // 인기도 (만점 10점)
            if (Popularity < 0)
            {
                totalScore = 100;
                Message.AppendLine("- 인기도가 음수입니다.");
            }
            else if (Popularity < 50)
            {
                totalScore += 10;
                Message.AppendLine("- 인기도가 낮습니다.");
            }
            else if (Popularity < 100)
            {
                totalScore += 5;
                Message.AppendLine("- 인기도가 소폭 낮습니다.");
            }

            // 레벨 (만점 15점)
            if (Level < 230)
            {
                totalScore = 100;
                Message.AppendLine("- 레벨이 매우 낮습니다.");
            }
            else if (Level < 250)
            {
                totalScore = 50;
                Message.AppendLine("- 레벨이 낮습니다.");
            }
            else if (Level < 260)
            {
                totalScore += 30;
                Message.AppendLine("- 레벨이 낮습니다.");
            }
            else if (Level < 265)
            {
                totalScore += 10;
            }
            else if (Level < 270)
            {
                totalScore += 5;
            }

            // 노블포인트 (만점 60)
            if (string.IsNullOrEmpty(Guild))
            {
                totalScore += 100;
                Message.AppendLine("- 가입된 길드가 확인되지 않습니다.");
            }
            else
            {
                if (GuildNoblessSP <= 30)
                {
                    totalScore += 100;
                    Message.AppendLine("- 가입된 길드의 규모가 매우 작습니다. (노블 포인트 30 이하)");
                }
                else if (GuildNoblessSP <= 35)
                {
                    totalScore += 60;
                    Message.AppendLine("- 가입된 길드의 규모가 작습니다. (노블 포인트 35 이하)");
                }
                else if (GuildNoblessSP <= 37)
                {
                    totalScore += 40;
                    Message.AppendLine("- 가입된 길드의 규모가 작습니다. (노블 포인트 37 이하)");
                }
                else if (GuildNoblessSP <= 39)
                {
                    totalScore += 30;
                    Message.AppendLine("- 가입된 길드의 규모가 작습니다. (노블 포인트 39 이하)");
                }
                else if (GuildNoblessSP <= 41)
                {
                    totalScore += 20;
                    Message.AppendLine("- 가입된 길드의 규모가 작습니다. (노블 포인트 41 이하)");
                }
                else if (GuildNoblessSP <= 43)
                {
                    totalScore += 15;
                    Message.AppendLine("- 가입된 길드의 규모가 작습니다. (노블 포인트 43 이하)");
                }
                else if (GuildNoblessSP <= 45)
                {
                    totalScore += 10;
                    Message.AppendLine("- 가입된 길드의 규모가 소폭 작습니다. (노블 포인트 45 이하)");
                }
            }

            // 최근 활동일 (만점 25점)
            if (ExpHistories == null || ExpHistories.Count < 5)
            {
                totalScore += 100;
                Message.AppendLine("- 최근에 생성된 캐릭터이거나 닉네임 변경 이력이 있습니다.");
            }
            else
            {
                bool isDateNull = false;
                foreach (var history in ExpHistories)
                {
                    if (history.Date == null)
                    {
                        isDateNull = true;
                        break;
                    }
                }

                if (isDateNull)
                {
                    Message.AppendLine("- (주의) 최근 활동 로그 추출 중 오류 발생.");
                    // TODO: DATE가 뭔가 비어있음
                }
                else
                {
                    double maxDelta = 0.0;
                    List<double> expGap = new List<double>();
                    for (int i = 0; i < ExpHistories.Count() - 1; i++)
                    {
                        DateTime? date1 = ExpHistories[i].Date;
                        DateTime? date2 = ExpHistories[i + 1].Date;

                        double delta = ((TimeSpan)(date1 - date2)).TotalDays;
                        if (maxDelta < delta)
                            maxDelta = delta;
                    }

                    // 현재 날짜와 제일 최근 경험치 날짜를 뺀 값도 포함
                    if (ExpHistories.Count() > 0)
                    {
                        double delta = ((TimeSpan)(DateTime.Now.Date - ExpHistories[ExpHistories.Count() - 1].Date)).TotalDays;
                        if (maxDelta < delta)
                            maxDelta = delta;
                    }

                    if (maxDelta > 364.0)
                    {
                        totalScore += 100;
                        Message.AppendLine("- 최근 1년 이상의 장기간 메접 이력이 있습니다.");
                    }
                    else if (maxDelta > 180.0)
                    {
                        totalScore += 100;
                        Message.AppendLine("- 최근 6개월 이상의 장기간 메접 이력이 있습니다.");
                    }
                    else if (maxDelta > 59.0)
                    {
                        totalScore += 100;
                        Message.AppendLine("- 최근 2개월 이상의 장기간 메접 이력이 있습니다.");
                    }
                    else if (maxDelta > 30.0)
                    {
                        totalScore += 50;
                        Message.AppendLine("- 최근 한 달 이상의 메접 이력이 있습니다.");
                    }
                    else if (maxDelta > 14.0)
                    {
                        totalScore += 20;
                        Message.AppendLine("- 최근 2주 이상의 메접 이력이 있습니다.");
                    }
                }
            }

            // 유니온 (만점 15점)
            if (!string.Equals(NickName, UnionMainCharacter))
            {
                totalScore += 100;
                Message.AppendLine("- 본캐릭터가 아닙니다. 본캐릭터 정보를 확인하시기 바랍니다.");
            }

            if (Union < 6000)
            {
                totalScore += 15;
                Message.AppendLine("- 유니온 레벨이 낮습니다.");
            }
            else if (Union < 7000)
            {
                totalScore += 10;
                Message.AppendLine("- 유니온 레벨이 다소 낮습니다.");
            }
            else if (Union < 8000)
            {
                totalScore += 5;
            }

            return 100 - totalScore < 0 ? 0 : 100 - totalScore; // 점수 합이 100점이 넘을 경우 100점으로 고정
        }
    }
}
