using Discord;
using IrisBot.Enums;
using IrisBot.Interfaces;
using Lavalink4NET.Rest;
using Lavalink4NET.Rest.Entities.Tracks;
using System;
using System.Configuration;
using System.Data.SQLite;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace IrisBot.Database
{
    public class GuildSettings : IGuildSettings
    {
        public ulong GuildId { get; }
        public float PlayerVolume { get; set; }
        public ulong? ListMessagdId { get; set; }
        public Translations Language { get; set; }
        public TrackSearchMode SearchPlatform { get; set; }
        public ulong? RoleMessageId { get; set; }
        public List<string> RoleEmojiIds { get; set; }
        public bool IsPrivateChannel { get; set; }

        private static List<GuildSettings>? GuildsList;

        // 처음 들어가는 채널 생성자
        public GuildSettings(ulong guildId, float playerVolume)
        {
            GuildId = guildId;
            PlayerVolume = playerVolume;
            ListMessagdId = null;
            Language = Translations.Korean;
            SearchPlatform = TrackSearchMode.YouTube;
            RoleMessageId = null;
            RoleEmojiIds = new List<string>();
            IsPrivateChannel = false;
        }

        // 저장된 데이터베이스 로드용 생성자
        public GuildSettings(ulong guildId, float playerVolume, Translations language, TrackSearchMode searchMode, ulong roleMessage, List<string> roleEmojiIds, bool isPrivate)
        {
            GuildId = guildId;
            PlayerVolume = playerVolume;
            ListMessagdId = null;
            Language = language;
            SearchPlatform = searchMode;
            RoleMessageId = roleMessage;
            RoleEmojiIds = roleEmojiIds;
            IsPrivateChannel = isPrivate;
        }

        public static List<GuildSettings> GetGuildsList()
        {
            if (GuildsList == null)
                GuildsList = new List<GuildSettings>();

            return GuildsList;
        }

        /// <summary>
        /// DB 파일이 없는 상태에서 실행시 테이블 생성을 하고 DB에서 값을 불러와 List<GuildSettings>에 저장한다
        /// </summary>
        /// <returns>void</returns>
        public static async Task InitializeAsync()
        {
            try
            {
                string connStr = @"DataSource=.\GuildSettings.db";
                string paths = AppDomain.CurrentDomain.BaseDirectory;
                AppDomain.CurrentDomain.SetData("DataDirectory", paths);

                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();
                    // 테이블이 있는지 없는지 확인한다.
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE NAME='Guilds'", conn))
                    {
                        var tableCount = await cmd.ExecuteScalarAsync();
                        if (tableCount == null || (long)tableCount == 0)
                        {
                            using (var createTable = new SQLiteCommand("CREATE TABLE Guilds(ID TEXT PRIMARY KEY, VOLUME REAL, LANG INTEGER, SEARCHMODE INTEGER, ROLEMESSAGE TEXT, ROLEEMOJI TEXT, ISPRIVATE INTEGER)", conn))
                            {
                                await CustomLog.PrintLog(LogSeverity.Warning, "Database", "Table \"Guilds\" does not exists. Creating new one.");
                                await createTable.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("SELECT * FROM Guilds", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ulong guildId = Convert.ToUInt64(reader["ID"]);
                            float volume = Convert.ToSingle(reader["VOLUME"]);
                            Translations lang = (Translations)Convert.ToInt32(reader["LANG"]);
                            TrackSearchMode mode = Convert.ToInt32(reader["SEARCHMODE"]) == 1 ? TrackSearchMode.YouTube : TrackSearchMode.SoundCloud;
                            List<string> roleEmojiIds = new List<string>();
                            ulong roleMessage = 0;
                            if (reader["ROLEMESSAGE"] != DBNull.Value) // NULL일 수 있는 값임
                                roleMessage = Convert.ToUInt64(reader["ROLEMESSAGE"]);

                            string roleEmojis = "";
                            if (reader["ROLEEMOJI"] != DBNull.Value) // NULL일 수 있는 값임
                                roleEmojis = (string)reader["ROLEEMOJI"];

                            List<string> output;
                            if (string.IsNullOrWhiteSpace(roleEmojis)) // DB 값이 DbNull 이거나 공백일 경우 비어있는 List 생성
                                output = new List<string>();
                            else
                                output = roleEmojis.Split("&").ToList();

                            bool isPrivate = Convert.ToBoolean(reader["ISPRIVATE"]);

                            for (int i = 0; i < output.Count; i++)
                            {
                                if (string.IsNullOrEmpty(output.ElementAt(i)))
                                    output.RemoveAt(i);
                            }

                            GetGuildsList().Add(new GuildSettings(guildId, volume, lang, mode, roleMessage, output, isPrivate));

                            await CustomLog.PrintLog(LogSeverity.Info, "Database", 
                                $"Load success (GuildId: {Convert.ToUInt64(reader["ID"])}, Language: {(Translations)Convert.ToInt32(reader["LANG"])})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// DB와 메모리 상의 List의 볼륨 값을 업데이트한다.
        /// </summary>
        /// <param name="volume">볼륨 값</param>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <returns>void</returns>
        public static async Task UpdateVolumeAsync(float volume, ulong guildId)
        {
            string connStr = @"DataSource=.\GuildSettings.db";
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.SetData("DataDirectory", paths);

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET VOLUME=@VOLUME WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@VOLUME", volume);
                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Changed volume to {volume} (GuildId: {guildId})");
                    }
                }

                GuildSettings? guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                    guild.PlayerVolume = volume;
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// DB와 메모리 상의 List의 언어 설정을 업데이트한다.
        /// </summary>
        /// <param name="language">언어</param>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <returns>void</returns>
        public static async Task UpdateLanguageAsync(Translations language, ulong guildId)
        {
            string connStr = @"DataSource=.\GuildSettings.db";
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.SetData("DataDirectory", paths);

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET LANG=@LANG WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@LANG", language);
                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Changed searchmode to {language} (GuildId: {guildId})");
                    }
                }

                GuildSettings? guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                    guild.Language = language;
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// DB와 메모리 상의 List의 검색 플랫폼 설정을 업데이트한다.
        /// </summary>
        /// <param name="searchMode">유튜브/사운드클라우드</param>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <returns>void</returns>
        public static async Task UpdateSearchModeAsync(TrackSearchMode searchMode, ulong guildId)
        {
            string connStr = @"DataSource=.\GuildSettings.db";
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.SetData("DataDirectory", paths);

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET SEARCHMODE=@SEARCHMODE WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@SEARCHMODE", searchMode == TrackSearchMode.YouTube ? 1 : 2);
                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Changed searchmode to {searchMode} (GuildId: {guildId})");
                    }
                }

                GuildSettings? guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                    guild.SearchPlatform = searchMode;
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        public static async Task UpdateIsPrivateAsync(bool isPrivate, ulong guildId)
        {
            string connStr = @"DataSource=.\GuildSettings.db";
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.SetData("DataDirectory", paths);

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET ISPRIVATE=@ISPRIVATE WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@ISPRIVATE", isPrivate);
                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Changed IsPrivate to {isPrivate} (GuildId: {guildId})");
                    }
                }

                GuildSettings? guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                    guild.IsPrivateChannel = isPrivate;
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// 이모지 반응 추가를 핸들링할 메세지를 지정하는 함수.
        /// </summary>
        /// <param name="roleMessageId">메세지 ID</param>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <returns></returns>
        public static async Task UpdateRoleMessageIdAsync(ulong roleMessageId, ulong guildId)
        {
            string connStr = @"DataSource=.\GuildSettings.db";
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.SetData("DataDirectory", paths);

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET ROLEMESSAGE=@ROLEMESSAGE WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@ROLEMESSAGE", roleMessageId.ToString());
                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Changed RoleMessage to {roleMessageId} (GuildId: {guildId})");
                    }
                }

                GuildSettings? guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                    guild.RoleMessageId = roleMessageId;
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// 역할 이모지를 추가하는 함수. 기본 이모지는 ID를 가지고 있지 않아 사용할 수 없음.
        /// </summary>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <param name="roleId">역할 ID</param>
        /// <param name="emojiId">이모지 ID</param>
        /// <returns></returns>
        public static async Task UpdateRoleEmojiIdsAsync(ulong guildId, ulong roleId, ulong emojiId)
        {
            string connStr = @"DataSource=.\GuildSettings.db";
            string paths = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.SetData("DataDirectory", paths);

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();
                    StringBuilder roleEmojis = new StringBuilder();
                    using (var cmd = new SQLiteCommand("SELECT ROLEEMOJI FROM Guilds WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (reader["ROLEEMOJI"] != DBNull.Value)
                                    roleEmojis.Append((string)reader["ROLEEMOJI"]);
                            }
                        }
                    }

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET ROLEEMOJI=@ROLEEMOJI WHERE ID=@ID", conn))
                    {
                        roleEmojis.Append($"{roleId}|{emojiId}&");
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@ROLEEMOJI", roleEmojis.ToString());
                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Added emoji role RoleID: {roleId}, EmojiID: {emojiId} (GuildId: {guildId})");
                    }
                }

                GuildSettings? guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                {
                    guild.RoleEmojiIds.Add($"{roleId}|{emojiId}");
                } 
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// 역할 이모지를 삭제하는 함수
        /// </summary>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <param name="roleId">역할 ID</param>
        /// <param name="emojiId">이모지 ID</param>
        /// <returns></returns>
        public static async Task RemoveEmojiAsync(ulong guildId, ulong roleId, ulong emojiId)
        {
            try
            {
                string connStr = @"DataSource=.\GuildSettings.db";
                string paths = AppDomain.CurrentDomain.BaseDirectory;
                AppDomain.CurrentDomain.SetData("DataDirectory", paths);

                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    StringBuilder roleEmojis = new StringBuilder();
                    using (var cmd = new SQLiteCommand("SELECT ROLEEMOJI FROM Guilds WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (reader["ROLEEMOJI"] != DBNull.Value)
                                    roleEmojis.Append((string)reader["ROLEEMOJI"]);
                            }
                        }
                    }
                    roleEmojis.Replace($"{roleId}|{emojiId}&", "");

                    using (var cmd = new SQLiteCommand("UPDATE Guilds SET ROLEEMOJI=@ROLEEMOJI WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        cmd.Parameters.AddWithValue("@ROLEEMOJI", roleEmojis.ToString());

                        await cmd.ExecuteNonQueryAsync();
                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Removed emoji role RoleID: {roleId}, EmojiID: {emojiId} (GuildId: {guildId})");
                    }

                    var guild = GetGuildsList().Find(x => x.GuildId == guildId);
                    if (guild != null)
                    {
                        string[] _roleEmojiIds = roleEmojis.ToString().Split("&");
                        guild.RoleEmojiIds = _roleEmojiIds.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        public static TrackSearchMode FindGuildSearchMode(ulong guildId)
        {
            GuildSettings? data = GetGuildsList().Find(x => x.GuildId == guildId);
            if (data?.SearchPlatform == null)
                return TrackSearchMode.YouTube;
            else
                return data.SearchPlatform;
        }

        public static ulong? FindRoleMessageId(ulong guildId)
        {
            GuildSettings? data = GetGuildsList().Find(x => x.GuildId == guildId);
            return data?.RoleMessageId;
        }

        public static List<string>? FindRoleEmojiIds(ulong guildId)
        {
            GuildSettings? data = GetGuildsList().Find(x => x.GuildId == guildId);
            return data?.RoleEmojiIds;
        }

        public static bool FindIsPrivateChannel(ulong guildId)
        {
            GuildSettings? data = GetGuildsList().Find(x => x.GuildId == guildId);
            if (data == null)
                return false;
            else
                return data.IsPrivateChannel;
        }

        /// <summary>
        /// DB와 List<GuildSettings>에 새로운 디스코드 서버 설정값을 추가하는 함수
        /// </summary>
        /// <param name="settings">길드 설정 정보</param>
        /// <returns>void</returns>
        public static async Task AddNewGuildAsync(GuildSettings settings)
        {
            try
            {
                string connStr = @"DataSource=.\GuildSettings.db";
                string paths = AppDomain.CurrentDomain.BaseDirectory;
                AppDomain.CurrentDomain.SetData("DataDirectory", paths);

                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SQLiteCommand("SELECT ID FROM Guilds WHERE ID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", settings.GuildId.ToString());
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (await reader.ReadAsync()) // 이미 등록된 GuildId의 데이터베이스가 있을 경우 취소한다.
                                return;
                        }
                    }

                    using (var cmd = new SQLiteCommand("INSERT INTO Guilds([ID], [VOLUME], [LANG], [SEARCHMODE], [ROLEMESSAGE], [ROLEEMOJI], [ISPRIVATE]) VALUES(@ID, @VOLUME, @LANG, @SEARCHMODE, @ROLEMESSAGE, @ROLEEMOJI, @ISPRIVATE)", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", settings.GuildId.ToString());
                        cmd.Parameters.AddWithValue("@VOLUME", 0.5f);
                        cmd.Parameters.AddWithValue("@LANG", settings.Language);
                        cmd.Parameters.AddWithValue("@SEARCHMODE", settings.SearchPlatform);
                        cmd.Parameters.AddWithValue("@ROLEMESSAGE", settings.RoleMessageId);

                        if (settings.RoleEmojiIds == null || settings.RoleEmojiIds.Count == 0)
                            cmd.Parameters.AddWithValue("@ROLEEMOJI", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@ROLEEMOJI", settings.RoleEmojiIds);

                        cmd.Parameters.AddWithValue("@ISPRIVATE", settings.IsPrivateChannel);
                        await cmd.ExecuteNonQueryAsync();
                        if (GetGuildsList().Find(x => x.GuildId == settings.GuildId) == null)
                            GetGuildsList().Add(settings);

                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"New database added successfully (GuildId: {settings.GuildId})");
                    }
                }                   
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }

        /// <summary>
        /// 봇의 디스코드 서버 퇴장 이벤트에서 데이터를 삭제하는 함수
        /// </summary>
        /// <param name="guildId">디스코드 서버 ID</param>
        /// <returns>void</returns>
        public static async Task RemoveGuildDataAsync(ulong guildId)
        {
            try
            {
                var guild = GetGuildsList().Find(x => x.GuildId == guildId);
                if (guild != null)
                    GetGuildsList().Remove(guild);

                string connStr = @"DataSource=.\GuildSettings.db";
                string paths = AppDomain.CurrentDomain.BaseDirectory;
                AppDomain.CurrentDomain.SetData("DataDirectory", paths);

                // 데이터베이스 자동 삭제
                using (var conn = new SQLiteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SQLiteCommand("DELETE FROM Guilds WHERE [ID]=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", guildId.ToString());
                        int result = await cmd.ExecuteNonQueryAsync();

                        await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Database removed successfully (GuildId: {guildId})");
                    }
                }

                // 플레이리스트 자동 삭제
                string path = Path.Combine(Program.PlaylistDirectory, guildId.ToString());
                DirectoryInfo di = new DirectoryInfo(path);
                if (di.Exists)
                {
                    foreach (var file in di.GetFiles())
                        file.Delete();
                    await CustomLog.PrintLog(LogSeverity.Info, "Database",
                            $"Playlist removed successfully (GuildId: {guildId})");
                }
            }
            catch (Exception ex)
            {
                await CustomLog.ExceptionHandler(ex);
            }
        }
    }
}
