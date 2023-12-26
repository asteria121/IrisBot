
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Security.Permissions;

namespace IrisBot
{
    public enum Jobs
    {
        [Description("기사단")]
        BKnights = 1, // 기사단
        [Description("소울마스터")]
        Soulmaster = 2,
        [Description("도적")] //직업군 분류
        BThief = 3,
        [Description("듀얼블레이더")]
        Dualblader = 4,
        [Description("마법사")] //직업군 분류
        BWizard = 5,
        [Description("비숍")]
        Bishop = 6,
        [Description("나이트로드")]
        Nightlord = 7,
        [Description("제로")]
        Zero = 8,
        [Description("해적")] //직업군 분류
        BPirate = 9,
        [Description("바이퍼")]
        Viper = 10,
        [Description("레지스탕스")]
        BResistance = 11,
        [Description("아크메이지(썬,콜)")]
        ArkmageTC = 12,
        [Description("아란")]
        Aran = 13,
        [Description("데몬슬레이어")]
        Demonslayer = 14,
        [Description("와일드헌터")]
        Wildhunter = 15,
        [Description("메르세데스")]
        Mercedes = 16,
        [Description("팬텀")]
        Phantom = 17,
        [Description("궁수")] // 직업군 분류
        BArcher = 18,
        [Description("보우마스터")]
        Bowmaster = 19,
        [Description("카이저")]
        Kaiser = 20,
        [Description("배틀메이지")]
        Battlemage = 21,
        [Description("전사")] //직업군 분류
        BWarrior = 22,
        [Description("다크나이트")]
        Darkknight = 23,
        [Description("아크")]
        Ark = 24,
        [Description("스트라이커")]
        Striker = 25,
        [Description("아크메이지(불,독)")]
        ArkmageFP = 26,
        [Description("윈드브레이커")]
        Windbreaker = 27,
        [Description("플레임위자드")]
        Flamewizard = 28,
        [Description("캐논마스터")]
        Cannonmaster = 29,
        [Description("히어로")]
        Hero = 30,
        [Description("은월")]
        Shade = 31,
        [Description("팔라딘")]
        Paladin = 32,
        [Description("메카닉")]
        Mechanic = 33,
        [Description("루미너스")]
        Luminous = 34,
        [Description("키네시스")]
        Kinesis = 35,
        [Description("섀도어")]
        Shadower = 36,
        [Description("나이트워커")]
        Nightwalker = 37,
        [Description("엔젤릭버스터")]
        Angelicbuster = 38,
        [Description("신궁")]
        Marks = 39,
        [Description("에반")]
        Evan = 40,
        [Description("캡틴")]
        Captain = 41,
        [Description("카데나")]
        Cadena = 42,
        [Description("블래스터")]
        Blaster = 43,
        [Description("일리움")]
        Illium = 44,
        [Description("시티즌")]
        Citzen = 45,
        [Description("노블레스")]
        Nobless = 46,
        [Description("초보자")]
        Newbie = 47,
        [Description("핑크빈")]
        Pinkbeen = 48,
        [Description("클레릭")]
        Cleric = 49,
        [Description("프리스트")]
        Priest = 50,
        [Description("헌터")]
        Hunter = 51,
        [Description("어쌔신")]
        Assassin = 52,
        [Description("레인저")]
        Ranger = 53,
        [Description("파이터")]
        Fighter = 54,
        [Description("나이트")]
        Knight = 55,
        [Description("메이지(썬,콜)")]
        MageTC = 56,
        [Description("페이지")]
        Page = 57,
        [Description("위자드(썬,콜)")]
        WizardTC = 58,
        [Description("허밋")]
        Hermit = 59,
        [Description("검사")]
        Swordman = 60,
        [Description("매지션")]
        Magician = 61,
        [Description("메이지(불,독)")]
        MageFP = 62,
        [Description("인파이터")]
        Infighter = 63,
        [Description("아처")]
        Archer = 64,
        [Description("캐논슈터")]
        Cannonshooter = 65,
        [Description("시프")]
        Thief = 66,
        [Description("위자드(불,독)")]
        WizardFP = 67,
        [Description("로그")]
        Rogue = 68,
        [Description("듀어러")]
        Dualer = 69,
        [Description("버서커")]
        Berserker = 70,
        [Description("시프마스터")]
        Thiefmaster = 71,
        [Description("크루세이더")]
        Crusader = 72,
        [Description("건슬링거")]
        Gunslinger = 73,
        [Description("슬래셔")]
        Slasher = 74,
        [Description("캐논블래스터")]
        CannonBlaster = 75,
        [Description("버커니어")]
        Buccaneer = 76,
        [Description("발키리")]
        Valkyrie = 77,
        [Description("스피어맨")]
        Spearman = 78,
        [Description("세미듀어러")]
        Semidualer = 79,
        [Description("저격수")]
        Sniper = 80,
        [Description("사수")]
        Marksman = 81,
        [Description("듀얼마스터")]
        Dualmaster = 82,
        [Description("??")]
        Unused = 83,
        [Description("제논")]
        Xenon = 84,
        [Description("데몬어벤져")]
        DemonAvenger = 85,
        [Description("미하일")]
        Mihile = 86,
        [Description("시티즌")] // 직업군 분류
        Citizen = 87,
        [Description("패스파인더")]
        Pathfinder = 90,
        [Description("에인션트아처")]
        Ancientarcher = 91,
        [Description("체이서")]
        Chaser = 92,
        [Description("아처(패스파인더)")]
        ArcherPF = 93,
        [Description("호영")]
        Hoyoung = 94,
        [Description("아델")]
        Adele = 95,
        [Description("카인")]
        Kain = 96,
        [Description("라라")]
        Lara = 97,
        [Description("칼리")]
        Khali = 98,
        [Description("프렌즈월드")]
        Friendsworld = 99,
        [Description("초월자")]
        Transcendent = 100,
    }
}
