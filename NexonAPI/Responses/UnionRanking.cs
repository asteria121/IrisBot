using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class UnionRankingBody
    {
        [JsonProperty("ranking")]
        public List<UnionRanking> Ranking;

        public string GetWorldMainCharacter(string worldname, string nickname)
        {
            if (Ranking != null && Ranking.Count > 0)
            {
                for (int i = 0; i < Ranking.Count; i++)
                {
                    if (string.Equals(worldname, Ranking[i].WorldName))
                    {
                        return Ranking[i].CharacterName;
                    }
                }
            }

            return "";
        }
    }

    public class UnionRanking
    {
        [JsonProperty("character_name")]
        public string CharacterName;
        [JsonProperty("ranking")]
        public int Ranking;
        [JsonProperty("world_name")]
        public string WorldName;
        [JsonProperty("union_level")]
        public int UnionLevel;
    }
}
