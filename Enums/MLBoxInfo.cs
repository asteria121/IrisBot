using Discord.Interactions;

namespace IrisBot
{
    public enum MLBoxInfo
    {
        [ChoiceDisplay("평범한 상자")]
        NormalBox,
        [ChoiceDisplay("조금 좋은 상자(A+)")]
        SpecialBox1,
        [ChoiceDisplay("조금 좋은 상자(S)")]
        SpecialBox2,
        [ChoiceDisplay("많이 좋은 상자")]
        GemBox,
        [ChoiceDisplay("쁘띠 루미너스 상자")]
        LuminousBox,
    }
}
