// ─────────────────────────────────────────────
// ArcanistSynergyStatusEffect: 비전 마법사 시너지 SSE.
// 시너지 보유 유닛의 MagicAttack에 arcanistSpellPower Flat 수정자를 적용한다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;
using Common.Data.Synergies;
using Scenes.Battle.Feature.Units.UnitStats;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 비전 마법사 시너지 SSE. 보유 유닛에 arcanistSpellPower 값의 MagicAttack Flat 수정자를 적용한다.
    /// 티어 상수: "arcanistSpellPower" (25, 50, 70)
    /// </summary>
    public class ArcanistSynergyStatusEffect : SynergyStatusEffect<SynergyStatusEffectContext>
    {
        public ArcanistSynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }
        private const string ArcanistSpellPowerKey = "arcanistSpellPower";

        protected override void OnSynergyActivated(SynergyTier tier)
        {
            ApplyModifier(tier);
        }

        protected override void OnSynergyTierChanged(SynergyTier newTier)
        {
            RemoveModifier();
            ApplyModifier(newTier);
        }

        protected override void OnSynergyDeactivated()
        {
            RemoveModifier();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            RemoveModifier();
        }

        /// <summary>티어 상수에서 arcanistSpellPower를 읽어 MagicAttack에 Flat 수정자를 추가한다.</summary>
        private void ApplyModifier(SynergyTier tier)
        {
            float? spellPower = tier.Get(ArcanistSpellPowerKey);
            if (!spellPower.HasValue) return;

            SynergyContext.Unit.StatSheet.MagicAttack.AddModifier(
                new StatModifier(this, StatModifierType.Flat, spellPower.Value));
        }

        /// <summary>이 SSE가 추가한 수정자를 모두 제거한다.</summary>
        private void RemoveModifier()
        {
            SynergyContext.Unit.StatSheet.MagicAttack.RemoveModifiersBySource(this);
        }
    }
}