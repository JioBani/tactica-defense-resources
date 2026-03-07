using Common.Data.Synergies;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지 1개의 카운트·티어 상태를 관리한다.
    /// SynergyManager가 산출한 유니크 카운트를 받아 임계치를 판정한다.
    /// </summary>
    public class SynergyState
    {
        private readonly SynergyDefinitionData _definition;
        private int _count;
        private SynergyTier? _activeTier;

        /// <summary>이 시너지의 정의 데이터.</summary>
        public SynergyDefinitionData Definition => _definition;

        /// <summary>현재 유니크 유닛 카운트.</summary>
        public int Count => _count;

        /// <summary>현재 활성 티어. 임계치 미달 시 null.</summary>
        public SynergyTier? ActiveTier => _activeTier;

        public SynergyState(SynergyDefinitionData definition)
        {
            _definition = definition;
        }

        /// <summary>
        /// 유니크 카운트를 받아 임계치를 갱신한다.
        /// </summary>
        /// <param name="uniqueCount">UnitDefinitionData.ID 기준 중복 제거된 유닛 수.</param>
        public void Recalculate(int uniqueCount)
        {
            _count = uniqueCount;
            _activeTier = FindActiveTier(_count);
            // TODO: 티어 변경 시 효과 적용/해제
        }

        /// <summary>
        /// 카운트에 해당하는 최고 활성 티어를 판정한다.
        /// tiers는 requiredCount 오름차순이어야 한다. 임계치 미달 시 null.
        /// </summary>
        private SynergyTier? FindActiveTier(int count)
        {
            var tiers = _definition.Tiers;
            if (tiers == null) return null;

            SynergyTier? result = null;

            foreach (var tier in tiers)
            {
                if (count >= tier.RequiredCount)
                    result = tier;
                else
                    break;
            }

            return result;
        }
    }
}