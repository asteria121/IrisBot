using Discord.Interactions;

namespace IrisBot
{
    public enum MLStats
    {
        [ChoiceDisplay("농장 관련 효과")]
        FarmStats,
        [ChoiceDisplay("주스탯 관련 효과")]
        Stats,
        [ChoiceDisplay("공격력/마력 관련 효과")]
        DamageStats,
        [ChoiceDisplay("크확/크뎀/데미지/보공/방무 관련 효과")]
        ImportantStats,
        [ChoiceDisplay("지속시간/재사용/파이널 어택 등 전투 관련 효과")]
        UtilityStats,
        [ChoiceDisplay("HP 증가 관련 효과")]
        HpStats,
        [ChoiceDisplay("사냥 관련 효과")]
        HuntStats,
        [ChoiceDisplay("기타 효과")]
        EtcStats,
    }
}
