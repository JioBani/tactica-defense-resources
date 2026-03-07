using System.Collections.Generic;
using Common.Data.Synergies;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>티어 변경 결과. 변경 여부와 이전 티어를 담는다.</summary>
    public readonly struct TierChangeResult
    {
        /// <summary>티어가 변경되었는지 여부.</summary>
        public readonly bool Changed;

        /// <summary>변경 전 티어. 이전에 비활성이었으면 null.</summary>
        public readonly SynergyTier? PreviousTier;

        public TierChangeResult(bool changed, SynergyTier? previousTier)
        {
            Changed = changed;
            PreviousTier = previousTier;
        }
    }

    /// <summary>
    /// 시너지 1개의 카운트·티어 상태를 관리한다.
    /// SynergyManager가 산출한 유니크 카운트를 받아 임계치를 판정한다.
    /// </summary>
    public class SynergyActivation
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

        public SynergyActivation(SynergyDefinitionData definition)
        {
            _definition = definition;
        }

        /// <summary>
        /// 유니크 카운트를 받아 임계치를 갱신한다.
        /// 이전 티어와 새 티어가 다르면 변경된 이전 티어를 반환하고, 동일하면 null을 반환한다.
        /// </summary>
        /// <param name="uniqueCount">UnitDefinitionData.ID 기준 중복 제거된 유닛 수.</param>
        /// <returns>티어가 변경되었으면 이전 티어(비활성→활성 시 null 포함), 변경 없으면 null.</returns>
        public TierChangeResult Recalculate(int uniqueCount)
        {
            _count = uniqueCount;
            SynergyTier? previousTier = _activeTier;
            _activeTier = FindActiveTier(_count);

            bool changed = previousTier?.Tier != _activeTier?.Tier;
            return new TierChangeResult(changed, previousTier);
        }

        /// <summary>
        /// 카운트에 해당하는 최고 활성 티어를 판정한다.
        /// tiers는 requiredCount 오름차순이어야 한다. 임계치 미달 시 null.
        /// </summary>
        private SynergyTier? FindActiveTier(int count)
        {
            IReadOnlyList<SynergyTier> tiers = _definition.Tiers;
            if (tiers == null) return null;

            SynergyTier? result = null;

            foreach (SynergyTier tier in tiers)
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