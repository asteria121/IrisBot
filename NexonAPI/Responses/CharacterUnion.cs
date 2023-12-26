using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class CharacterUnion
    {
        [JsonProperty("union_level")]
        public long UnionLevel { get; set; }
        [JsonProperty("union_grade")]
        public string UnionGrade { get; set; }

        public bool IsNull()
        {
            if (UnionGrade == null)
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return UnionLevel.ToString();
        }
    }
}
