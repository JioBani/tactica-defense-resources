using System;
using System.Collections.Generic;
using System.Linq;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 목록 패널. 사전 배치된 인디케이터를 시너지 수만큼 활성화하고 클릭 이벤트를 상위에 전달한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SynergyListPanel : MonoBehaviour
    {
        /// <summary>에디터에서 사전 배치된 인디케이터 배열.</summary>
        [SerializeField] private SynergyIndicator[] indicators;

        /// <summary>인디케이터 클릭 시 해당 SynergyActivation을 전달한다.</summary>
        public event Action<SynergyActivation> OnIndicatorClicked;

        private void Start()
        {
            // SynergyManager.Start()에서 초기화된 시너지 목록을 읽는다
            IReadOnlyDictionary<Common.Data.Synergies.SynergyDefinitionData, SynergyActivation> activations =
                SynergyManager.Instance.SynergyActivations;

            // 시너지 수만큼 인디케이터를 바인딩한다
            int index = 0;
            foreach (SynergyActivation activation in activations.Values)
            {
                if (index < indicators.Length)
                {
                    indicators[index].Bind(activation);
                    // 카운트가 1 이상인 시너지만 표시한다
                    indicators[index].gameObject.SetActive(activation.Count > 0);
                    indicators[index].OnClicked += HandleIndicatorClicked;
                    index++;
                }
            }

            // 사용하지 않는 인디케이터는 비활성화한다
            for (int i = index; i < indicators.Length; i++)
            {
                indicators[i].gameObject.SetActive(false);
            }

            // 초기 정렬 (정렬 로직은 TACD-297에서 구현)
            SortIndicators();
        }

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated);
        }

        /// <summary>시너지 재계산 이벤트 수신 시 가시성을 갱신하고 정렬을 실행한다.</summary>
        private void HandleSynergyRecalculated(OnSynergyRecalculatedEventDto dto)
        {
            // 카운트에 따라 인디케이터 활성/비활성을 토글한다
            for (int i = 0; i < indicators.Length; i++)
            {
                if (indicators[i].BoundActivation != null)
                {
                    indicators[i].gameObject.SetActive(indicators[i].BoundActivation.Count > 0);
                }
            }

            SortIndicators();
        }

        /// <summary>인디케이터를 활성 상태·진행률·티어·카운트 기준으로 정렬한다.</summary>
        private void SortIndicators()
        {
            // 활성(gameObject.activeSelf == true) 인디케이터만 수집한다
            List<SynergyIndicator> activeIndicators = indicators
                .Where(indicator => indicator.gameObject.activeSelf)
                .ToList();

            // LINQ OrderBy 안정 정렬: 활성 > 진행률 > 티어 단계 > 카운트
            List<SynergyIndicator> sorted = activeIndicators
                .OrderByDescending(indicator => indicator.BoundActivation.ActiveTier.Value.HasValue ? 1 : 0)
                .ThenByDescending(indicator =>
                {
                    if (indicator.BoundActivation.ActiveTier.Value.HasValue)
                    {
                        return CalculateTierProgress(indicator.BoundActivation);
                    }
                    return -1f;
                })
                .ThenByDescending(indicator =>
                {
                    if (indicator.BoundActivation.ActiveTier.Value.HasValue)
                    {
                        return indicator.BoundActivation.ActiveTier.Value.Value.Tier;
                    }
                    return -1;
                })
                .ThenByDescending(indicator => indicator.BoundActivation.Count)
                .ToList();

            // 정렬 결과 순서대로 SetSiblingIndex를 호출하여 LayoutGroup 순서를 반영한다
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].transform.SetSiblingIndex(i);
            }
        }

        /// <summary>정렬에 사용할 활성 시너지의 티어 진행률을 계산한다.</summary>
        private float CalculateTierProgress(SynergyActivation activation)
        {
            IReadOnlyList<Common.Data.Synergies.SynergyTier> tiers = activation.Definition.Tiers;
            Common.Data.Synergies.SynergyTier activeTier = activation.ActiveTier.Value.Value;

            // 현재 활성 티어의 인덱스를 찾는다
            int activeTierIndex = -1;
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].Tier == activeTier.Tier)
                {
                    activeTierIndex = i;
                    break;
                }
            }

            // 다음 티어가 존재하면 진행률을 계산하고, 없으면 최고 티어 달성이므로 1.0을 반환한다
            int nextTierIndex = activeTierIndex + 1;
            if (nextTierIndex < tiers.Count)
            {
                return (float)activation.Count / tiers[nextTierIndex].RequiredCount;
            }
            else
            {
                return 1.0f;
            }
        }

        /// <summary>인디케이터 클릭을 수신하여 상위 패널에 전달한다. (CD-9)</summary>
        private void HandleIndicatorClicked(SynergyActivation activation)
        {
            OnIndicatorClicked?.Invoke(activation);
        }
    }
}
