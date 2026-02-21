using Common.Data.Skills;
using Common.Data.Units.UnitStatsByLevel;
using NUnit.Framework;
using Scenes.Battle.Feature.Units.Damage;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;

namespace Tests.Editor
{
    public class SkillCoefficientCalculatorTests
    {
        private UnitStatSheet _statSheet;

        [SetUp]
        public void SetUp()
        {
            _statSheet = new UnitStatSheet();
            _statSheet.MagicAttack.SetBaseValue(100f);
            _statSheet.PhysicalAttack.SetBaseValue(50f);
            _statSheet.AttackSpeed.SetBaseValue(1.2f);
        }

        [Test]
        public void BaseValueOnly_NullScalings_ReturnsBaseValue()
        {
            // scalings가 null이면 baseValue만 반환
            var coefficient = new SkillCoefficient { baseValue = 30f, scalings = null };

            float result = SkillCoefficientCalculator.Calculate(coefficient, _statSheet);

            Assert.AreEqual(30f, result);
        }

        [Test]
        public void BaseValueOnly_EmptyScalings_ReturnsBaseValue()
        {
            // scalings가 빈 배열이면 baseValue만 반환
            var coefficient = new SkillCoefficient
            {
                baseValue = 50f,
                scalings = new StatScaling[0]
            };

            float result = SkillCoefficientCalculator.Calculate(coefficient, _statSheet);

            Assert.AreEqual(50f, result);
        }

        [Test]
        public void SingleScaling_AddsStatTimesCoefficient()
        {
            // 50 + (100 * 1.5) = 200
            var coefficient = new SkillCoefficient
            {
                baseValue = 50f,
                scalings = new[]
                {
                    new StatScaling { statKind = UnitStatKind.MagicAttack, coefficient = 1.5f }
                }
            };

            float result = SkillCoefficientCalculator.Calculate(coefficient, _statSheet);

            Assert.AreEqual(200f, result);
        }

        [Test]
        public void MultipleScalings_SumsAll()
        {
            // 10 + (100 * 1.0) + (50 * 0.5) = 10 + 100 + 25 = 135
            var coefficient = new SkillCoefficient
            {
                baseValue = 10f,
                scalings = new[]
                {
                    new StatScaling { statKind = UnitStatKind.MagicAttack, coefficient = 1.0f },
                    new StatScaling { statKind = UnitStatKind.PhysicalAttack, coefficient = 0.5f }
                }
            };

            float result = SkillCoefficientCalculator.Calculate(coefficient, _statSheet);

            Assert.AreEqual(135f, result);
        }

        [Test]
        public void InvalidStatKind_SkipsNullStat()
        {
            // Get()이 null을 반환하는 잘못된 statKind는 무시
            // 20 + (100 * 2.0) = 220 (잘못된 스탯은 건너뜀)
            var coefficient = new SkillCoefficient
            {
                baseValue = 20f,
                scalings = new[]
                {
                    new StatScaling { statKind = (UnitStatKind)999, coefficient = 5.0f },
                    new StatScaling { statKind = UnitStatKind.MagicAttack, coefficient = 2.0f }
                }
            };

            float result = SkillCoefficientCalculator.Calculate(coefficient, _statSheet);

            Assert.AreEqual(220f, result);
        }
    }
}
