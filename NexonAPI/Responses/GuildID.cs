using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class GuildID
    {
        [JsonProperty("oguild_id")]
        public string Id { get; set; }

        public bool IsNull()
        {
            if (Id == null)
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
