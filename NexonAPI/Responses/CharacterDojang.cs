using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class CharacterDojang
    {
        [JsonProperty("dojang_best_floor")]
        public long BestFloor { get; set; }
        [JsonProperty("date_dojang_record")]
        public string DateDojangRecord { get; set; }
        [JsonProperty("dojang_best_time")]
        public long BestTime { get; set; }


        public bool IsNull()
        {
            if (string.IsNullOrEmpty(DateDojangRecord))
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return BestFloor.ToString();
        }
    }
}
