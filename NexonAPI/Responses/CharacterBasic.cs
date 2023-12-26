using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class CharacterBasic
    {
        [JsonProperty("character_name")]
        public string Nickname { get; set; }
        [JsonProperty("world_name")]
        public string WorldName { get; set; }
        [JsonProperty("character_class")]
        public string Class { get; set; }
        [JsonProperty("character_class_level")]
        public string ClassLevel { get; set; }
        [JsonProperty("character_level")]
        public int Level { get; set; }
        [JsonProperty("character_exp")]
        public ulong Exp { get; set; }
        [JsonProperty("character_exp_rate")]
        public string ExpRate { get; set; }
        [JsonProperty("character_guild_name")]
        public string GuildName { get; set; }

        public bool IsNull()
        {
            if (Nickname == null)
                return true;
            else if (WorldName == null)
                return true;
            else if (Class == null)
                return true;
            else if (ClassLevel == null)
                return true;
            else if (ExpRate == null)
                return true;
            else if (GuildName == null)
                return true;

            return false;
        }
    }
}
