using System;
using System.Collections.Generic;
using Common.Data.Synergies;
using Scenes.Battle.Feature.Synergy;
using UnityEngine.UIElements;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// 시너지 상세 패널의 효과 정보 섹션 뷰.
    /// 표시 대상 시너지의 이름·효과 설명(플레이스홀더 원본 그대로)·티어 임계치·티어별 효과 수치를
    /// 정의 저장소에서 조회하여 표시하고, 활성 티어를 다른 티어와 구분되는 시각 상태로 둔다.
    /// 표시 상태 동안 시너지의 활성 티어 변경 알림(RxValue OnChange)을 구독하여 강조 표시를 갱신한다.
    /// </summary>
    public class SynergyDetailEffectSection
    {
        private const string TierItemClass = "tier-item";
        private const string TierItemActiveClass = "tier-item--active";
        private const string TierItemStageClass = "tier-item__stage";
        private const string TierItemThresholdClass = "tier-item__threshold";
        private const string TierItemConstantsClass = "tier-item__constants";
        private const string TierItemConstantClass = "tier-item__constant";

        private readonly VisualElement _sectionRoot;
        private readonly Label _nameLabel;
        private readonly Label _descriptionLabel;
        private readonly VisualElement _tierListContainer;

        private readonly Dictionary<int, VisualElement> _tierItemsByStage = new();

        private SynergyActivation _activation;
        private Action<SynergyTier?> _activeTierChangedHandler;

        public SynergyDetailEffectSection(VisualElement sectionRoot)
        {
            _sectionRoot = sectionRoot;
            _nameLabel = sectionRoot?.Q<Label>("synergy-name");
            _descriptionLabel = sectionRoot?.Q<Label>("synergy-description");
            _tierListContainer = sectionRoot?.Q<VisualElement>("tier-list");
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
            ClearTierItems();
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

            ClearTierItems();

            IReadOnlyList<SynergyTier> tiers = definition.Tiers;
            if (tiers == null || _tierListContainer == null)
            {
                return;
            }

            // 정의의 Tiers 는 requiredCount 오름차순으로 유지된다는 SynergyTier 컨벤션을 따른다.
            foreach (SynergyTier tier in tiers)
            {
                VisualElement item = CreateTierItem(tier);
                _tierListContainer.Add(item);
                _tierItemsByStage[tier.Tier] = item;
            }
        }

        private VisualElement CreateTierItem(SynergyTier tier)
        {
            var item = new VisualElement();
            item.AddToClassList(TierItemClass);

            var stage = new Label($"{tier.Tier}단계");
            stage.AddToClassList(TierItemStageClass);
            item.Add(stage);

            var threshold = new Label($"{tier.RequiredCount}명");
            threshold.AddToClassList(TierItemThresholdClass);
            item.Add(threshold);

            var constantsBox = new VisualElement();
            constantsBox.AddToClassList(TierItemConstantsClass);
            if (tier.Constants != null)
            {
                foreach (KeyValuePair<string, float> entry in tier.Constants)
                {
                    var line = new Label($"{entry.Key} {entry.Value}");
                    line.AddToClassList(TierItemConstantClass);
                    constantsBox.Add(line);
                }
            }
            item.Add(constantsBox);

            return item;
        }

        // ── 활성 티어 강조 ──

        private void RenderActiveTier(SynergyTier? active)
        {
            foreach (KeyValuePair<int, VisualElement> kv in _tierItemsByStage)
            {
                kv.Value.RemoveFromClassList(TierItemActiveClass);
            }

            if (active.HasValue && _tierItemsByStage.TryGetValue(active.Value.Tier, out VisualElement item))
            {
                item.AddToClassList(TierItemActiveClass);
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

        private void ClearTierItems()
        {
            _tierListContainer?.Clear();
            _tierItemsByStage.Clear();
        }
    }
}
