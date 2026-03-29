using Common.Data.Summoners.SummonerDefinitions;
using Common.Data.Units.UnitLoadOuts;
using UnityEngine;

namespace Common.Data.Summoners.SummonerLoadOuts
{
    /// <summary>소환술사의 인게임 복합 에셋. 소환술사 정의와 유닛 전투 데이터를 연결한다.</summary>
    [CreateAssetMenu(menuName = "Summoners/SummonerLoadOutData", fileName = "SummonerLoadOutData", order = 0)]
    public class SummonerLoadOutData : UnitLoadOutData
    {
        [Header("소환술사")]
        [Tooltip("소환술사 정의 데이터")]
        [SerializeField] private SummonerDefinitionData summoner;
        /// <summary>소환술사 정의 참조.</summary>
        public SummonerDefinitionData Summoner => summoner;
    }
}