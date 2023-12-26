using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class CharacterPopularity
    {
        [JsonProperty("popularity")]
        public long Popularity { get; set; }

        public override string ToString()
        {
            return Popularity.ToString();
        }
    }
}
