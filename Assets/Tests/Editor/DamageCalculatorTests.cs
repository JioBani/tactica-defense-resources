using NUnit.Framework;
using Scenes.Battle.Feature.Units.Damage;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;

namespace Tests.Editor
{
    public class DamageCalculatorTests
    {
        private UnitStatSheet _attacker;
        private UnitStatSheet _victim;

        [SetUp]
        public void SetUp()
        {
            _attacker = new UnitStatSheet();
            _attacker.PhysicalAttack.SetBaseValue(100f);
            _attacker.MagicAttack.SetBaseValue(80f);
            _attacker.CriticalChance.SetBaseValue(0f);
            _attacker.CriticalDamageMultiplier.SetBaseValue(1.5f);
            _attacker.DamageDealtIncrease.SetBaseValue(0f);

            _victim = new UnitStatSheet();
            _victim.PhysicalDefense.SetBaseValue(0f);
            _victim.MagicDefense.SetBaseValue(0f);
            _victim.DamageReduction.SetBaseValue(0f);
        }

        [Test]
        public void BaseDamage_NoCrit_NoDef()
        {
            // 100 * 1.0 = 100
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false);
            Assert.AreEqual(100f, result);
        }

        [Test]
        public void MagicDamage_UseMagicAttack()
        {
            // 마법 공격력 80 사용
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Magical, isCritical: false);
            Assert.AreEqual(80f, result);
        }

        [Test]
        public void SkillCoefficient_MultipliesBase()
        {
            // 100 * 1.5 = 150
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false, skillCoefficient: 1.5f);
            Assert.AreEqual(150f, result);
        }

        [Test]
        public void CriticalHit_MultipliesByCritDamage()
        {
            // 100 * 1.5(치명타) = 150
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: true);
            Assert.AreEqual(150f, result);
        }

        [Test]
        public void Defense_ReducesDamage()
        {
            // 100 * (1 - 0.3) = 70
            _victim.PhysicalDefense.SetBaseValue(0.3f);
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false);
            Assert.AreEqual(70f, result);
        }

        [Test]
        public void MagicDefense_AppliedForMagicDamage()
        {
            // 80 * (1 - 0.25) = 60
            _victim.MagicDefense.SetBaseValue(0.25f);
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Magical, isCritical: false);
            Assert.AreEqual(60f, result);
        }

        [Test]
        public void DamageIncrease_MultipliesDamage()
        {
            // 100 * (1 + 0.2) = 120
            _attacker.DamageDealtIncrease.SetBaseValue(0.2f);
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false);
            Assert.AreEqual(120f, result);
        }

        [Test]
        public void DamageReduction_ReducesDamage()
        {
            // 100 * (1 - 0.1) = 90
            _victim.DamageReduction.SetBaseValue(0.1f);
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false);
            Assert.AreEqual(90f, result);
        }

        [Test]
        public void FullCombo_AllFactors()
        {
            // 100 * 1.5(crit) * (1-0.3)(def) * (1+0.2)(inc) * (1-0.1)(red)
            // = 100 * 1.5 * 0.7 * 1.2 * 0.9 = 113.4 → floor = 113
            _victim.PhysicalDefense.SetBaseValue(0.3f);
            _attacker.DamageDealtIncrease.SetBaseValue(0.2f);
            _victim.DamageReduction.SetBaseValue(0.1f);

            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: true);
            Assert.AreEqual(113f, result);
        }

        [Test]
        public void Floor_TruncatesDecimal()
        {
            // 100 * (1 - 0.33) = 67.0 → 67 (정수 결과도 검증)
            _victim.PhysicalDefense.SetBaseValue(0.33f);
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false);
            Assert.AreEqual(67f, result);
        }

        [Test]
        public void MinimumZero_NeverNegative()
        {
            // 방어력이 100% 이상이면 최소 0
            _victim.PhysicalDefense.SetBaseValue(1.5f);
            float result = DamageCalculator.Calculate(_attacker, _victim, DamageType.Physical, isCritical: false);
            Assert.AreEqual(0f, result);
        }
    }
}
