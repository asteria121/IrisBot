using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class CharacterOCID
    {
        [JsonProperty("ocid")]
        public string Ocid { get; set; }

        public bool IsNull()
        {
            if (Ocid == null)
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return Ocid.ToString();
        }
    }
}
