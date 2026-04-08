using System;
using System.Collections.Generic;
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

        /// <summary>인디케이터 정렬을 수행한다. 정렬 로직은 TACD-297에서 구현한다.</summary>
        private void SortIndicators()
        {
            // TACD-297에서 구현 예정
        }

        /// <summary>인디케이터 클릭을 수신하여 상위 패널에 전달한다. (CD-9)</summary>
        private void HandleIndicatorClicked(SynergyActivation activation)
        {
            OnIndicatorClicked?.Invoke(activation);
        }
    }
}
