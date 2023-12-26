using Newtonsoft.Json;

namespace IrisBot.NexonAPI.Responses
{
    public class ErrorBody
    {
        [JsonProperty("error")]
        public Error Error;
    }

    public class Error
    {
        [JsonProperty("message")]
        public string Message;

        [JsonProperty("name")]
        public string Name;
    }
}