// ─────────────────────────────────────────────
// ArcanistSpellPowerEffect: 비전 마법사 시너지의 부수 효과.
// 시너지 비보유 유닛의 MagicAttack에 spellPower Flat 수정자를 적용한다.
// SSE가 아니므로 시너지 소속 정체성을 나타내지 않는다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Units.UnitStats;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 비전 마법사 시너지의 부수 효과. 비보유 유닛에 spellPower 값의 MagicAttack Flat 수정자를 적용한다.
    /// TierLinkedStatusEffect를 상속하여 ActiveTier 구독으로 생명주기를 자동 관리한다.
    /// 티어 상수: "spellPower" (15, 20, 35)
    /// </summary>
    public class ArcanistSpellPowerEffect : TierLinkedStatusEffect<TierLinkedStatusEffectContext>
    {
        public ArcanistSpellPowerEffect(StatusEffectDefinitionData definition) : base(definition) { }
        private const string SpellPowerKey = "spellPower";

        protected override void OnTierActivated(SynergyTier tier)
        {
            ApplyModifier(tier);
        }

        protected override void OnTierChanged(SynergyTier newTier)
        {
            RemoveModifier();
            ApplyModifier(newTier);
        }

        protected override void OnTierDeactivated()
        {
            RemoveModifier();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            RemoveModifier();
        }

        /// <summary>티어 상수에서 spellPower를 읽어 MagicAttack에 Flat 수정자를 추가한다.</summary>
        private void ApplyModifier(SynergyTier tier)
        {
            float? spellPower = tier.Get(SpellPowerKey);
            if (!spellPower.HasValue) return;

            TierContext.Unit.StatSheet.MagicAttack.AddModifier(
                new StatModifier(this, StatModifierType.Flat, spellPower.Value));
        }

        /// <summary>이 SE가 추가한 수정자를 모두 제거한다.</summary>
        private void RemoveModifier()
        {
            TierContext.Unit.StatSheet.MagicAttack.RemoveModifiersBySource(this);
        }
    }
}