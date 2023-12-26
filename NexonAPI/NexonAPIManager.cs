using System.Web;
using IrisBot.NexonAPI.Responses;
using IrisBot.Enums;
using Newtonsoft.Json;

namespace IrisBot.NexonAPI
{
    public static class NexonAPIManager<T>
    {
        private static readonly string jsonNullParseStr = "JSON 파싱 결과가 NULL 입니다.";

        public static async Task<T> GetResultAsync(HttpClient client, Uri requestUri)
        {
            var response = await client.GetAsync(requestUri);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<T>(body);
                if (result == null)
                {
                    Exception ex = new Exception(jsonNullParseStr);
                    throw ex;
                }
                else
                    return result;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<ErrorBody>(body);
                if (result == null)
                {
                    Exception ex = new Exception(jsonNullParseStr);
                    throw ex;
                }
                else
                    throw new NexonAPIExceptions(result);
            }
        }
    }

    public static class NexonAPIManager
    {
        private static readonly string baseUrl = "https://open.api.nexon.com";

        private static DateTimeOffset Now
        {
            get
            {
                return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"));
            }
        }

        private static string ToDateString(DateTimeOffset minDate, DateTimeOffset date)
        {
            var minYear = minDate.Year;
            var minMonth = minDate.Month;
            var minDay = minDate.Day;

            var tmp = date.Subtract(TimeSpan.FromDays(1));
            var year = tmp.Year;
            var month = tmp.Month;
            var day = tmp.Day;

            if (year < minYear || (year == minYear && month < minMonth) || (year == minYear && month == minMonth && day < minDay))
            {
                throw new ArgumentException($"You can only retrieve data after {minYear}-{minMonth:D2}-{minDay:D2}.");
            }

            var yyyyMMdd = new DateTime(year, month, day).ToString("yyyy-MM-dd");

            return yyyyMMdd;
        }

        private static DateTimeOffset MinDate(int year, int month, int day)
        {
            return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.FromHours(9));
        }

        private static void SetClientInformation(HttpClient client)
        {
            client.Timeout = TimeSpan.FromMilliseconds(5000);
            client.DefaultRequestHeaders.Add("x-nxopen-api-key", Program.OpenApiKey);
        }

        public static async Task<CharacterOCID> GetUserOcidAsync(string nickname)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/id");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["character_name"] = nickname;
                uri.Query = query.ToString();

                return await NexonAPIManager<CharacterOCID>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<CharacterBasic> GetCharacterBasicAsync(string ocid)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/character/basic");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["ocid"] = ocid;
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                uri.Query = query.ToString();

                return await NexonAPIManager<CharacterBasic>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<CharacterPopularity> GetCharacterPopularityAsync(string ocid)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/character/popularity");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["ocid"] = ocid;
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                uri.Query = query.ToString();

                return await NexonAPIManager<CharacterPopularity>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<CharacterUnion> GetCharacterUnionAsync(string ocid)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/user/union");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["ocid"] = ocid;
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                uri.Query = query.ToString();

                return await NexonAPIManager<CharacterUnion>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<UnionRankingBody> GetCharacterUnionRankingAsync(string worldName, string ocid)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/ranking/union");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                query["world_name"] = worldName;
                query["ocid"] = ocid;
                uri.Query = query.ToString();

                return await NexonAPIManager<UnionRankingBody>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<CharacterDojang> GetCharacterDojangAsync(string ocid)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/character/dojang");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["ocid"] = ocid;
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                uri.Query = query.ToString();

                return await NexonAPIManager<CharacterDojang>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<GuildID> GetGuildIdAsync(string guildName, string worldName)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/guild/id");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["guild_name"] = guildName;
                query["world_name"] = worldName;
                uri.Query = query.ToString();

                return await NexonAPIManager<GuildID>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<GuildBasic> GetGuildInformationAsync(string guildId)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/guild/basic");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["oguild_id"] = guildId;
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                uri.Query = query.ToString();

                return await NexonAPIManager<GuildBasic>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<GuildRankingBody> GetGuildRankingAsync(string worldName, GuildRankType rankingType, string guildName)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/ranking/guild");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                query["world_name"] = worldName;
                query["ranking_type"] = ((int)rankingType).ToString();
                query["guild_name"] = guildName;
                uri.Query = query.ToString();

                return await NexonAPIManager<GuildRankingBody>.GetResultAsync(client, uri.Uri);
            }
        }

        public static async Task<CharacterStatBody> GetCharacterStatAsync(string ocid)
        {
            using (HttpClient client = new HttpClient())
            {
                SetClientInformation(client);

                var uri = new UriBuilder($"{baseUrl}/maplestory/v1/character/stat");
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["ocid"] = ocid;
                query["date"] = ToDateString(MinDate(2023, 12, 21), Now);
                uri.Query = query.ToString();

                return await NexonAPIManager<CharacterStatBody>.GetResultAsync(client, uri.Uri);
            }
        }
    }
}
