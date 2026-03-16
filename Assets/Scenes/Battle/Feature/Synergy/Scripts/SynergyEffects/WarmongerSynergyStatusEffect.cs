// ─────────────────────────────────────────────
// WarmongerSynergyStatusEffect: 전쟁기계 시너지 SSE.
// 체력 50% 기준 조건부 DamageReduction + IOnActionStateChangedHook으로 사망 시 아군 회복.
// ─────────────────────────────────────────────
using System.Linq;
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.BubbleMessage;
using Common.Scripts.StatusEffect.HookProvider;
using Scenes.Battle.Feature.Unit.Defenders;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Units.UnitStats;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 전쟁기계 시너지 효과. 체력 50% 이상이면 높은 DamageReduction, 미만이면 낮은 값을 적용한다.
    /// 자신이 다운되면 다른 전쟁기계를 회복시킨다.
    /// </summary>
    public class WarmongerSynergyStatusEffect
        : SynergyStatusEffect<SynergyStatusEffectContext>, IOnActionStateChangedHook
    {
        private const string LowDamageReductionKey = "lowDamageReduction";
        private const string HighDamageReductionKey = "highDamageReduction";
        private const string HealPercentKey = "healPercent";
        private const float HealthThreshold = 0.5f;

        private readonly DefenderManager _defenderManager;

        private float _lowReduction;
        private float _highReduction;
        private float _healPercent;
        private bool _isAboveThreshold;
        private Units.Unit _unit;

        public WarmongerSynergyStatusEffect(
            StatusEffectDefinitionData definition,
            DefenderManager defenderManager) : base(definition)
        {
            _defenderManager = defenderManager;
        }

        protected override void OnSynergyActivated(SynergyTier tier)
        {
            CacheTierValues(tier);
            _unit = SynergyContext.Unit;
            _unit.StatSheet.Health.OnChange += OnHealthChanged;
            EvaluateThresholdAndApply();
        }

        protected override void OnSynergyTierChanged(SynergyTier newTier)
        {
            CacheTierValues(newTier);
            RemoveModifier();
            ApplyModifier();
        }

        protected override void OnSynergyDeactivated()
        {
            Cleanup();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Cleanup();
        }

        // ── IOnActionStateChangedHook ──

        /// <summary>Downed 진입 시 다른 전쟁기계를 회복시킨다.</summary>
        public void OnActionStateEnter(ActionStateType stateType)
        {
            if (stateType != ActionStateType.Downed) return;
            HealOtherWarmongers();
        }

        public void OnActionStateExit(ActionStateType stateType) { }

        // ── 내부 로직 ──

        /// <summary>체력 변화 시 50% 임계값을 재평가한다.</summary>
        private void OnHealthChanged(float newHealth)
        {
            bool wasAbove = _isAboveThreshold;
            _isAboveThreshold = newHealth / _unit.StatSheet.MaxHealth.CurrentValue >= HealthThreshold;

            if (wasAbove != _isAboveThreshold)
            {
                RemoveModifier();
                ApplyModifier();
            }
        }

        /// <summary>현재 체력 비율을 평가하고 수정자를 적용한다.</summary>
        private void EvaluateThresholdAndApply()
        {
            _isAboveThreshold =
                _unit.StatSheet.Health.Value / _unit.StatSheet.MaxHealth.CurrentValue >= HealthThreshold;
            ApplyModifier();
        }

        /// <summary>현재 임계값에 맞는 DamageReduction 수정자를 추가한다.</summary>
        private void ApplyModifier()
        {
            float value = _isAboveThreshold ? _highReduction : _lowReduction;
            _unit.StatSheet.DamageReduction.AddModifier(
                new StatModifier(this, StatModifierType.Flat, value));
        }

        /// <summary>이 SSE가 추가한 수정자를 모두 제거한다.</summary>
        private void RemoveModifier()
        {
            _unit.StatSheet.DamageReduction.RemoveModifiersBySource(this);
        }

        /// <summary>다른 전쟁기계 Defender를 최대 체력의 healPercent만큼 회복시킨다.</summary>
        private void HealOtherWarmongers()
        {
            if (_defenderManager == null) return;

            var defenders = _defenderManager.GetBattleAreaDefenders();
            foreach (var defender in defenders)
            {
                if ((Units.Unit)defender == _unit) continue;
                if (!HasWarmongerSynergy(defender)) continue;

                var statSheet = defender.StatSheet;
                float healAmount = statSheet.MaxHealth.CurrentValue * _healPercent;
                statSheet.SetCurrentHealth(statSheet.Health.Value + healAmount);

                BubbleMessageSpawner.Instance.SpawnAtWorld(
                    $"+{healAmount:F0}",
                    defender.transform.position,
                    new BubbleMessageParams(color: Color.green));
            }
        }

        /// <summary>Defender가 전쟁기계 시너지를 보유하고 있는지 확인한다.</summary>
        private static bool HasWarmongerSynergy(Defender defender)
        {
            return defender.UnitLoadOutData.Unit.Synergies
                .Any(s => s.Id == SynergyId.Warmonger);
        }

        /// <summary>티어 상수를 캐싱한다.</summary>
        private void CacheTierValues(SynergyTier tier)
        {
            _lowReduction = tier.Get(LowDamageReductionKey) ?? 0f;
            _highReduction = tier.Get(HighDamageReductionKey) ?? 0f;
            _healPercent = tier.Get(HealPercentKey) ?? 0.05f;
        }

        /// <summary>구독 해제 및 수정자 제거.</summary>
        private void Cleanup()
        {
            if (_unit != null)
                _unit.StatSheet.Health.OnChange -= OnHealthChanged;
            RemoveModifier();
        }
    }
}
