using IrisBot;
using IrisBot.NexonAPI.Responses;
using System.Text;

namespace IrisBot.Interfaces
{
    public interface IUser
    {
        public string NickName { get; }
        public string World { get; }
        public long Level { get; }
        public string Job { get; }
        public long Popularity { get; }
        public long DojangFloor { get; }
        public long Union { get; }
        public string UnionMainCharacter { get; }
        public string CombatPower { get; }
        public List<UnionRanking> UnionRankings { get; }
        public List<ExpHistory>? ExpHistories { get; }
        public string Guild { get; }
        public long GuildFameRank { get; }
        public long GuildRaceRank { get; }
        public long GuildPunchRank { get; }
        public long GuildSize { get; }
        public long GuildNoblessSP { get; }
        public int Score { get; }
        public StringBuilder Message { get; }
    }
}

