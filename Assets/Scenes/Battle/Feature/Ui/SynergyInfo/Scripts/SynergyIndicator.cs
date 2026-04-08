using System;
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 인디케이터 1개의 UI를 담당한다.
    /// 바인딩된 SynergyActivation의 아이콘과 티어 도트 fillAmount를 표시하고,
    /// 시너지 재계산 이벤트 수신 시 갱신한다.
    /// </summary>
    public class SynergyIndicator : MonoBehaviour
    {
        [SerializeField] private Image icon;

        /// <summary>티어 도트의 외부 GameObject 배열. SetActive 제어용.</summary>
        [SerializeField] private GameObject[] tierDots;

        [SerializeField] private Button button;

        /// <summary>tierDots 자식에서 런타임에 가져온 inner RectTransform 배열.</summary>
        private RectTransform[] _innerRects;

        private SynergyActivation _activation;

        /// <summary>클릭 시 바인딩된 SynergyActivation을 전달한다.</summary>
        public event Action<SynergyActivation> OnClicked;

        /// <summary>현재 바인딩된 SynergyActivation.</summary>
        public SynergyActivation BoundActivation => _activation;

        /// <summary>시너지 데이터를 바인딩하고 UI를 초기화한다.</summary>
        public void Bind(SynergyActivation activation)
        {
            _activation = activation;

            icon.sprite = activation.Definition.Icon;

            // tierDots 자식에서 inner RectTransform을 가져온다
            _innerRects = new RectTransform[tierDots.Length];
            for (int i = 0; i < tierDots.Length; i++)
            {
                _innerRects[i] = tierDots[i].transform.GetChild(0) as RectTransform;
            }

            // 티어 도트 활성/비활성 초기화
            int tierCount = Math.Min(activation.Definition.Tiers.Count, tierDots.Length);
            for (int i = 0; i < tierDots.Length; i++)
            {
                tierDots[i].SetActive(i < tierCount);
            }

            RefreshDisplay();

            GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated);
        }

        /// <summary>티어 도트의 inner 너비 비율을 현재 카운트에 따라 갱신한다.</summary>
        private void RefreshDisplay()
        {
            IReadOnlyList<SynergyTier> tiers = _activation.Definition.Tiers;
            int count = _activation.Count;

            int dotCount = Math.Min(tiers.Count, _innerRects.Length);
            for (int i = 0; i < dotCount; i++)
            {
                int rangeStart = i == 0 ? 0 : tiers[i - 1].RequiredCount;
                int rangeEnd = tiers[i].RequiredCount;

                float ratio;
                if (count >= rangeEnd)
                {
                    ratio = 1f;
                }
                else if (count <= rangeStart)
                {
                    ratio = 0f;
                }
                else
                {
                    ratio = (float)(count - rangeStart) / (rangeEnd - rangeStart);
                }

                // inner의 anchorMax.x를 비율로 설정하여 너비를 조정한다
                _innerRects[i].anchorMax = new Vector2(ratio, _innerRects[i].anchorMax.y);
            }
        }

        /// <summary>시너지 재계산 이벤트 수신 시 UI를 갱신한다.</summary>
        private void HandleSynergyRecalculated(OnSynergyRecalculatedEventDto dto)
        {
            if (dto.Activation == _activation)
            {
                RefreshDisplay();
            }
        }

        /// <summary>클릭 이벤트를 처리하여 상위 패널에 전달한다.</summary>
        private void HandleClick()
        {
            OnClicked?.Invoke(_activation);
        }

        private void Awake()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            GlobalEventBus.Unsubscribe<OnSynergyRecalculatedEventDto>(HandleSynergyRecalculated);
        }
    }
}
