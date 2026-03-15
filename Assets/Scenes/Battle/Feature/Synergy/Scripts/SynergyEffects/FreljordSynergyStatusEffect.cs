// ─────────────────────────────────────────────
// FreljordSynergyStatusEffect: 프렐요드 시너지 SSE.
// IOnAttackHitHook을 구현하여 공격 적중 시 대상에게 FreljordFrostEffect(둔화)를 부여한다.
// HookProvider가 이 인터페이스를 감지하여 자동으로 캐싱/해제한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.StatusEffect.HookProvider;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.Attackables;
using UnitType = Scenes.Battle.Feature.Units.Unit;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 프렐요드 시너지 효과. 공격 적중 시 대상에게 서리 둔화를 부여한다.
    /// 티어별 둔화 수치: (2) 20% / (4) 35% / (6) 60%, 지속 3초.
    /// </summary>
    public class FreljordSynergyStatusEffect : SynergyStatusEffect<SynergyStatusEffectContext>, IOnAttackHitHook
    {
        private const string SlowPercentKey = "slowPercent";
        private const string SlowDurationKey = "slowDuration";

        private float _slowPercent;
        private float _slowDuration;

        /// <summary>피해자별 활성 둔화 효과 추적. 재적중 시 Refresh, 비활성화 시 일괄 제거.</summary>
        private readonly Dictionary<UnitType, FreljordFrostEffect> _activeFrostEffects = new();

        public FreljordSynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }

        protected override void OnSynergyActivated(SynergyTier tier)
        {
            CacheTierValues(tier);
        }

        protected override void OnSynergyTierChanged(SynergyTier newTier)
        {
            CacheTierValues(newTier);

            foreach (var frostEffect in _activeFrostEffects.Values)
                frostEffect.UpdateSlowPercent(_slowPercent);
        }

        protected override void OnSynergyDeactivated()
        {
            RemoveAllFrostEffects();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            RemoveAllFrostEffects();
        }

        /// <summary>공격 적중 시 호출된다. 대상에게 서리 둔화를 부여하거나 리프레시한다.</summary>
        public void OnAttackHit(Victim victim)
        {
            var victimUnit = victim.Unit;

            if (_activeFrostEffects.TryGetValue(victimUnit, out var existing) && !existing.IsExpired)
            {
                existing.Refresh(_slowDuration);
                return;
            }

            var frostEffect = new FreljordFrostEffect(Definition.StatusEffectDefinition, _slowPercent, _slowDuration);
            var context = new FreljordFrostEffectContext(victimUnit.StatSheet.MoveSpeed);
            victimUnit.StatusEffectController.Apply(frostEffect, context);
            _activeFrostEffects[victimUnit] = frostEffect;
        }

        /// <summary>티어 상수를 캐싱한다.</summary>
        private void CacheTierValues(SynergyTier tier)
        {
            _slowPercent = tier.Get(SlowPercentKey) ?? 0f;
            _slowDuration = tier.Get(SlowDurationKey) ?? 3f;
        }

        /// <summary>추적 중인 모든 둔화 효과를 제거한다.</summary>
        private void RemoveAllFrostEffects()
        {
            foreach (var kvp in _activeFrostEffects)
            {
                var frostEffect = kvp.Value;
                if (frostEffect.IsExpired) continue;
                frostEffect.Controller?.RemoveImmediate(frostEffect);
            }

            _activeFrostEffects.Clear();
        }
    }
}
