namespace Common.Data.Synergies
{
    /// <summary>시너지 종류. 소환술사 효과와 소환수 특성을 구분한다.</summary>
    public enum SynergyType
    {
        /// <summary>소환술사 효과. 소속 소환술사에 의해 고정된다.</summary>
        SummonerEffect,

        /// <summary>소환수 특성. 전장마다 랜덤으로 부여된다.</summary>
        SummonTrait,
    }
}
