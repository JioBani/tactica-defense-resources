using Common.Data.Skills;
using Common.Data.Skills.SkillDefinitions;
using Common.Data.Units.UnitDefinitions;
using Common.Data.Units.UnitStatsByLevel;
using UnityEngine;

namespace Common.Data.Units.UnitLoadOuts
{
    [CreateAssetMenu( menuName = "Units/UnitLoadOutData", fileName = "UnitLoadOutData", order = 0)]
    public class UnitLoadOutData : ScriptableObject
    {
        //TODO: enum 으로 변경
        [Header("표시 정보 (Identity)")]
        [Tooltip("로드아웃 ID")]
        [SerializeField] private int id;
        public int ID => id;
        
        [Tooltip("유닛 정의 데이터")]
        [SerializeField] private UnitDefinitionData unit;
        public UnitDefinitionData  Unit => unit;
        
        [Tooltip("스킬 정의 데이터")]
        [SerializeField] private SkillDefinitionData skill;
        public SkillDefinitionData  Skill => skill;
        
        [Tooltip("유닛 스탯 데이터")]
        [SerializeField] private UnitStatsByLevelData stats;
        public UnitStatsByLevelData  Stats => stats;

        [Header("성급별 소환 비용")]
        [Tooltip("성급별 마나 소비량. 인덱스 = 성급 (0성은 미사용)")]
        [SerializeField] private int[] costByStar = { 0, 1, 3, 9 };

        [Header("성급별 환원 비용")]
        [Tooltip("성급별 환원 마나. 인덱스 = 성급 (0성은 미사용). 강화 보너스는 별도 가산된다.")]
        [SerializeField] private int[] refundByStar = { 0, 1, 2, 8 };

        /// <summary>해당 성급의 소환 비용을 반환한다. 범위 밖이면 1성 비용을 반환한다.</summary>
        public int GetCostByStar(int star)
        {
            if (star < 0 || star >= costByStar.Length)
                return costByStar[1];
            return costByStar[star];
        }

        /// <summary>
        /// 해당 성급·강화 단계의 환원 마나를 반환한다.
        /// 환원 마나 = refundByStar[star] + reinforcement.
        /// 범위 밖이면 1성 환원 비용을 반환한다.
        /// </summary>
        public int GetRefundMana(int star, int reinforcement = 0)
        {
            //TODO : star < 0, star >= refundByStar.Length 인 경우는 에러 리포트하도록 처리
            int baseRefund = (star < 0 || star >= refundByStar.Length)
                ? refundByStar[1]
                : refundByStar[star];
            return baseRefund + reinforcement;
        }
    }
}