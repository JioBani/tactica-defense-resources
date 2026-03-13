// ─────────────────────────────────────────────
// BruiserSynergyStatusEffect: 난동꾼 시너지. 보유 유닛의 MaxHealth에 티어별 % 수정자를 적용한다.
// ─────────────────────────────────────────────
using Common.Data.Synergies;
using Scenes.Battle.Feature.Units.UnitStats;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 난동꾼 시너지 효과. MaxHealth에 티어별 % 수정자를 적용한다.
    /// 티어 상수: "healthPercent" (0.25, 0.40, 0.60)
    /// </summary>
    public class BruiserSynergyStatusEffect : SynergyStatusEffect<SynergyStatusEffectContext>
    {
        private const string HealthPercentKey = "healthPercent";

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

        /// <summary>티어 상수에서 healthPercent를 읽어 MaxHealth에 Percent 수정자를 추가한다.</summary>
        private void ApplyModifier(SynergyTier tier)
        {
            float? percent = tier.Get(HealthPercentKey);
            if (!percent.HasValue) return;

            SynergyContext.Unit.StatSheet.MaxHealth.AddModifier(
                new StatModifier(this, StatModifierType.Percent, percent.Value));
        }

        /// <summary>이 SSE가 추가한 수정자를 모두 제거한다.</summary>
        private void RemoveModifier()
        {
            SynergyContext.Unit.StatSheet.MaxHealth.RemoveModifiersBySource(this);
        }
    }
}
