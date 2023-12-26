using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class CharacterStatBody
    {
        [JsonProperty("final_stat")]
        public List<FinalStat> CharacterStat;

        public string GetCombatPower()
        {
            for (int i = 0; i < CharacterStat.Count(); i++)
            {
                if (string.Equals("전투력", CharacterStat[i].StatName))
                    return CharacterStat[i].StatValue;
            }

            return "";
        }
    }

    public class FinalStat
    {
        [JsonProperty("stat_name")]
        public string StatName;
        [JsonProperty("stat_value")]
        public string StatValue;
    }
}
