// ─────────────────────────────────────────────
// GunslingerSynergyStatusEffect: 총잡이 시너지 SSE.
// 공격력 % 버프 + IOnAttackHitHook으로 4회 공격마다 추가 물리 피해.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.BubbleMessage;
using Common.Scripts.StatusEffect.HookProvider;
using Scenes.Battle.Feature.Units.Attackables;
using Scenes.Battle.Feature.Units.UnitStats;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 총잡이 시너지 효과. 공격력 % 버프를 적용하고,
    /// 기본 공격 4회마다 추가 물리 피해를 입힌다.
    /// </summary>
    public class GunslingerSynergyStatusEffect
        : SynergyStatusEffect<SynergyStatusEffectContext>, IOnAttackHitHook
    {
        private const string AttackPercentKey = "attackPercent";
        private const string ExtraDamageKey = "extraDamage";
        private const int HitsPerProc = 4;

        private float _attackPercent;
        private float _extraDamage;
        private int _attackCount;
        private Units.Unit _unit;

        public GunslingerSynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }

        protected override void OnSynergyActivated(SynergyTier tier)
        {
            CacheTierValues(tier);
            _unit = SynergyContext.Unit;
            _attackCount = 0;
            ApplyModifier();
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

        // ── IOnAttackHitHook ──

        /// <summary>공격 적중 시 카운트를 증가시키고, 4회마다 추가 물리 피해를 입힌다.</summary>
        public void OnAttackHit(AttackContext context)
        {
            if (context.Source == this) return;

            _attackCount++;
            if (_attackCount < HitsPerProc) return;

            _attackCount = 0;

            // Source=this로 AttackContext를 생성하여 자기 proc의 재카운트를 방지
            var victim = context.Victim;
            var procContext = new AttackContext(_extraDamage, context.Attacker, victim, source: this);
            victim.Hit(procContext);

            BubbleMessageSpawner.Instance?.SpawnAtWorld(
                $"{_extraDamage:F0}!",
                victim.transform.position,
                new BubbleMessageParams(color: Color.yellow));
        }

        // ── 내부 로직 ──

        /// <summary>PhysicalAttack에 % 수정자를 적용한다.</summary>
        private void ApplyModifier()
        {
            _unit.StatSheet.PhysicalAttack.AddModifier(
                new StatModifier(this, StatModifierType.Percent, _attackPercent));
        }

        /// <summary>이 SSE가 추가한 수정자를 모두 제거한다.</summary>
        private void RemoveModifier()
        {
            _unit.StatSheet.PhysicalAttack.RemoveModifiersBySource(this);
        }

        /// <summary>티어 상수를 캐싱한다.</summary>
        private void CacheTierValues(SynergyTier tier)
        {
            _attackPercent = tier.Get(AttackPercentKey) ?? 0f;
            _extraDamage = tier.Get(ExtraDamageKey) ?? 0f;
        }

        /// <summary>수정자 제거 및 카운트 초기화.</summary>
        private void Cleanup()
        {
            RemoveModifier();
            _attackCount = 0;
        }
    }
}
