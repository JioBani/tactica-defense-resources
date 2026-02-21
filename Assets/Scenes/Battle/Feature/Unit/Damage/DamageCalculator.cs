using Common.Data.Damage;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.Damage
{
    /// <summary>
    /// 기획서 1.3 데미지 계산 공식.
    /// 순서: 기본 데미지 → 치명타 → 방어력 → 피해 증가 → 피해 감소 → 소수점 버림(최소 0)
    /// </summary>
    public static class DamageCalculator
    {
        public static float Calculate(
            UnitStatSheet attackerStats,
            UnitStatSheet victimStats,
            DamageType damageType,
            float skillCoefficient = 1f)
        {
            bool isCritical = Random.value < attackerStats.CriticalChance.CurrentValue;
            return Calculate(attackerStats, victimStats, damageType, isCritical, skillCoefficient);
        }

        public static float Calculate(
            UnitStatSheet attackerStats,
            UnitStatSheet victimStats,
            DamageType damageType,
            bool isCritical,
            float skillCoefficient = 1f)
        {
            // 1. 기본 데미지
            float baseDamage = damageType == DamageType.Physical
                ? attackerStats.PhysicalAttack.CurrentValue
                : attackerStats.MagicAttack.CurrentValue;

            baseDamage *= skillCoefficient;

            // 2. 치명타
            if (isCritical)
                baseDamage *= attackerStats.CriticalDamageMultiplier.CurrentValue;

            // 3. 방어력 적용
            float defense = damageType == DamageType.Physical
                ? victimStats.PhysicalDefense.CurrentValue
                : victimStats.MagicDefense.CurrentValue;
            baseDamage *= (1f - defense);

            // 4. 피해 증가 (공격자)
            baseDamage *= (1f + attackerStats.DamageDealtIncrease.CurrentValue);

            // 5. 피해 감소 (피격자)
            baseDamage *= (1f - victimStats.DamageReduction.CurrentValue);

            // 6. 소수점 버림, 최소 0
            return Mathf.Max(0f, Mathf.Floor(baseDamage));
        }
    }
}
