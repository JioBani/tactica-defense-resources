using System;
using System.Collections.Generic;
using Common.Data.Synergies;
using Scenes.Battle.Feature.Synergy;
using UnityEngine.UIElements;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널의 효과 정보 섹션 뷰.
    /// 표시 대상 시너지의 이름·효과 설명·티어 진행도를 정의 저장소에서 조회하여 표시한다.
    /// 진행도는 각 티어의 임계 카운트를 노드로, 노드 사이를 connector로 시각화하고,
    /// 현재 카운트와 활성 티어를 기준으로 passed/active/next 시각 상태를 적용한다.
    /// 표시 상태 동안 활성 티어 변경 알림(RxValue OnChange)을 구독하여 갱신한다.
    /// </summary>
    public class SynergyDetailEffectSection
    {
        private const string NodeClass = "progress-node";
        private const string NodePassedClass = "progress-node--passed";
        private const string NodeActiveClass = "progress-node--active";
        private const string NodeNextClass = "progress-node--next";
        private const string ConnectorClass = "progress-connector";
        private const string ConnectorLitClass = "progress-connector--lit";
        private const string ConnectorDimLitClass = "progress-connector--dim-lit";

        private readonly VisualElement _sectionRoot;
        private readonly Label _nameLabel;
        private readonly Label _descriptionLabel;
        private readonly VisualElement _progressContainer;

        /// <summary>티어 인덱스 → 노드 Label 매핑. RenderActiveTier에서 시각 상태를 재적용하기 위해 보관한다.</summary>
        private readonly List<Label> _nodes = new();
        /// <summary>노드 사이 connector 매핑. 인덱스 i 는 노드 i 와 i+1 사이 connector.</summary>
        private readonly List<VisualElement> _connectors = new();

        private SynergyActivation _activation;
        private Action<SynergyTier?> _activeTierChangedHandler;

        public SynergyDetailEffectSection(VisualElement sectionRoot)
        {
            _sectionRoot = sectionRoot;
            _nameLabel = sectionRoot?.Q<Label>("synergy-name");
            _descriptionLabel = sectionRoot?.Q<Label>("synergy-description");
            _progressContainer = sectionRoot?.Q<VisualElement>("tier-list");
        }

        /// <summary>표시 대상 설정 + 정적 정보 렌더 + 활성 티어 강조 + 알림 구독 개시.</summary>
        public void Bind(SynergyActivation activation)
        {
            if (activation == null)
            {
                return;
            }

            _activation = activation;
            RenderStatic();
            RenderActiveTier(activation.ActiveTier.Value);
            SubscribeActiveTier();
        }

        /// <summary>표시 상태 유지 교체 — 이전 구독 해제 + 새 시너지 렌더·구독.</summary>
        public void Rebind(SynergyActivation activation)
        {
            Unbind();
            Bind(activation);
        }

        /// <summary>알림 구독 해제 + 표시 정리.</summary>
        public void Unbind()
        {
            UnsubscribeActiveTier();
            ClearProgress();
            if (_nameLabel != null) _nameLabel.text = string.Empty;
            if (_descriptionLabel != null) _descriptionLabel.text = string.Empty;
            _activation = null;
        }

        // ── 정적 정보 렌더 ──

        private void RenderStatic()
        {
            SynergyDefinitionData definition = _activation.Definition;

            if (_nameLabel != null)
            {
                _nameLabel.text = definition.DisplayName ?? string.Empty;
            }
            if (_descriptionLabel != null)
            {
                // 플레이스홀더(@Const@ / @Const*N@) 원본 그대로 — 본 이슈에서 치환 없음 (DoD-S8).
                _descriptionLabel.text = definition.Description ?? string.Empty;
            }

            ClearProgress();

            IReadOnlyList<SynergyTier> tiers = definition.Tiers;
            if (tiers == null || _progressContainer == null)
            {
                return;
            }

            // 정의의 Tiers 는 requiredCount 오름차순으로 유지된다는 SynergyTier 컨벤션을 따른다.
            for (int i = 0; i < tiers.Count; i++)
            {
                if (i > 0)
                {
                    VisualElement connector = CreateConnector();
                    _progressContainer.Add(connector);
                    _connectors.Add(connector);
                }

                Label node = CreateNode(tiers[i]);
                _progressContainer.Add(node);
                _nodes.Add(node);
            }
        }

        private Label CreateNode(SynergyTier tier)
        {
            // 기본 라벨은 RequiredCount 한 자리 — 활성 노드는 "n/N" 으로 RenderActiveTier 에서 갱신한다.
            var node = new Label(tier.RequiredCount.ToString());
            node.AddToClassList(NodeClass);
            return node;
        }

        private VisualElement CreateConnector()
        {
            var connector = new VisualElement();
            connector.AddToClassList(ConnectorClass);
            return connector;
        }

        // ── 활성 티어 강조 ──

        /// <summary>현재 카운트와 활성 티어를 기준으로 각 노드·connector 의 시각 상태를 재계산한다.</summary>
        private void RenderActiveTier(SynergyTier? active)
        {
            if (_activation == null)
            {
                return;
            }

            IReadOnlyList<SynergyTier> tiers = _activation.Definition.Tiers;
            if (tiers == null)
            {
                return;
            }

            int activeIndex = -1;
            if (active.HasValue)
            {
                for (int i = 0; i < tiers.Count; i++)
                {
                    if (tiers[i].Tier == active.Value.Tier)
                    {
                        activeIndex = i;
                        break;
                    }
                }
            }

            // 노드 상태: 활성 위치 기준으로 passed / active / next 분기
            for (int i = 0; i < _nodes.Count; i++)
            {
                Label node = _nodes[i];
                node.RemoveFromClassList(NodePassedClass);
                node.RemoveFromClassList(NodeActiveClass);
                node.RemoveFromClassList(NodeNextClass);

                if (activeIndex < 0)
                {
                    // 활성 티어 없음 — 모든 노드는 미도달 outlined 상태로 표기한다.
                    node.text = tiers[i].RequiredCount.ToString();
                    node.AddToClassList(NodeNextClass);
                }
                else if (i < activeIndex)
                {
                    // 활성 이전 티어 — 통과 상태 (muted blue).
                    node.text = tiers[i].RequiredCount.ToString();
                    node.AddToClassList(NodePassedClass);
                }
                else if (i == activeIndex)
                {
                    // 활성 티어 — "현재카운트/임계치" 형태로 강조.
                    node.text = $"{_activation.Count}/{tiers[i].RequiredCount}";
                    node.AddToClassList(NodeActiveClass);
                }
                else
                {
                    // 활성 이후 미도달 티어.
                    node.text = tiers[i].RequiredCount.ToString();
                    node.AddToClassList(NodeNextClass);
                }
            }

            // connector 상태: 양 끝 노드가 모두 통과/활성이면 lit, 통과끼리만 이으면 dim-lit
            for (int i = 0; i < _connectors.Count; i++)
            {
                VisualElement connector = _connectors[i];
                connector.RemoveFromClassList(ConnectorLitClass);
                connector.RemoveFromClassList(ConnectorDimLitClass);

                if (activeIndex < 0)
                {
                    continue;
                }

                // connector i 는 노드 i 와 i+1 사이.
                bool leftReached = i < activeIndex || i == activeIndex;
                bool rightReached = i + 1 < activeIndex || i + 1 == activeIndex;

                if (leftReached && rightReached)
                {
                    // 양쪽 다 통과/활성 — dim-lit (이미 도달한 구간).
                    connector.AddToClassList(ConnectorDimLitClass);
                }
            }
        }

        // ── 알림 구독 ──

        private void SubscribeActiveTier()
        {
            _activeTierChangedHandler = newTier => RenderActiveTier(newTier);
            _activation.ActiveTier.OnChange += _activeTierChangedHandler;
        }

        private void UnsubscribeActiveTier()
        {
            if (_activation != null && _activeTierChangedHandler != null)
            {
                _activation.ActiveTier.OnChange -= _activeTierChangedHandler;
            }
            _activeTierChangedHandler = null;
        }

        // ── 정리 ──

        private void ClearProgress()
        {
            _progressContainer?.Clear();
            _nodes.Clear();
            _connectors.Clear();
        }
    }
}
