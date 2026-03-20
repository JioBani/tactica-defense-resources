using System;
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.Rxs;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지 1개의 카운트·티어 상태를 관리한다.
    /// Recalculate 호출 시 티어 변경을 감지하여 활성화/변경/비활성화 이벤트를 발행한다.
    /// </summary>
    public class SynergyActivation
    {
        private readonly SynergyDefinitionData _definition;
        private int _count;
        private readonly RxValue<SynergyTier?> _activeTier = new(null);

        /// <summary>이 시너지의 정의 데이터.</summary>
        public SynergyDefinitionData Definition => _definition;

        /// <summary>현재 유니크 유닛 카운트.</summary>
        public int Count => _count;

        /// <summary>현재 활성 티어. RxValue로 OnChange 구독이 가능하다. 임계치 미달 시 Value가 null.</summary>
        public RxValue<SynergyTier?> ActiveTier => _activeTier;

        /// <summary>시너지가 비활성에서 활성으로 전환될 때 발행된다. null → tier</summary>
        public event Action<SynergyTier> OnTierActivated;

        /// <summary>시너지가 활성 상태에서 티어만 변경될 때 발행된다. tier → 다른 tier</summary>
        public event Action<SynergyTier> OnTierChanged;

        /// <summary>시너지가 활성에서 비활성으로 전환될 때 발행된다. tier → null</summary>
        public event Action OnTierDeactivated;

        public SynergyActivation(SynergyDefinitionData definition)
        {
            _definition = definition;
        }

        /// <summary>
        /// 유니크 카운트를 받아 임계치를 갱신한다.
        /// RxValue.OnChange로 SSE에 알린 뒤, 티어 변경 이벤트를 발행한다.
        /// </summary>
        /// <param name="uniqueCount">UnitDefinitionData.ID 기준 중복 제거된 유닛 수.</param>
        public void Recalculate(int uniqueCount)
        {
            _count = uniqueCount;
            SynergyTier? previousTier = _activeTier.Value;
            _activeTier.Value = FindActiveTier(_count);

            bool tierChanged = previousTier?.Tier != _activeTier.Value?.Tier;
            if (tierChanged)
            {
                if (!previousTier.HasValue && _activeTier.Value.HasValue)
                {
                    OnTierActivated?.Invoke(_activeTier.Value.Value);
                }
                else if (previousTier.HasValue && !_activeTier.Value.HasValue)
                {
                    OnTierDeactivated?.Invoke();
                }
                else if (previousTier.HasValue && _activeTier.Value.HasValue)
                {
                    OnTierChanged?.Invoke(_activeTier.Value.Value);
                }
            }
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