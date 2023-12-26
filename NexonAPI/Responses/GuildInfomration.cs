using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class GuildInformation   
    {
        public GuildBasic Basic;
        public GuildRankingBody FameRanking;
        public GuildRankingBody RaceRanking;
        public GuildRankingBody PunchRanking;
    }
    public class GuildBasic
    {
        [JsonProperty("world_name")]
        public string WorldName;
        [JsonProperty("guild_name")]
        public string GuildName;
        [JsonProperty("guild_member_count")]
        public long GuildMemberCount;
        // TODO: NEXON Open API 페이지는 guild_nobless_skill 인데 실제 API 결과는 guild_noblesse_skill로 되어있음?
        [JsonProperty("guild_noblesse_skill")]
        public List<GuildNoblessSkills> NoblessSkills;

        public long GetGuildNoblessSP()
        {
            if (NoblessSkills == null || NoblessSkills.Count == 0)
            {
                return 0;
            }
            else
            {
                long sp = 0; // 총 노블레스 스킬 포인트
                for (int i =0; i < NoblessSkills.Count; i++)
                    sp += NoblessSkills[i].SkillLevel;

                return sp;
            }
        }
    }

    public class GuildNoblessSkills
    {
        [JsonProperty("skill_level")]
        public long SkillLevel;
    }

    public class GuildRankingBody
    {
        [JsonProperty("ranking")]
        public List<GuildRanking> Ranking;
    }

    public class GuildRanking
    {
        [JsonProperty("ranking")]
        public int Ranking;
        [JsonProperty("guild_point")]
        public long GuildPoint;
    }
}