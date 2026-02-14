using NUnit.Framework;
using Scenes.Battle.Feature.Units.UnitStats;

namespace Tests.Editor
{
    public class UnitStatTests
    {
        // ── Additive 계산 ──

        [Test]
        public void SetBaseValue_SetsCurrentValue()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            Assert.AreEqual(100f, stat.CurrentValue);
            Assert.AreEqual(100f, stat.BaseValue);
        }

        [Test]
        public void Additive_FlatModifier_AddsToBase()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            stat.AddModifier(new StatModifier("buff", StatModifierType.Flat, 20f));
            Assert.AreEqual(120f, stat.CurrentValue);
        }

        [Test]
        public void Additive_PercentModifier_ScalesBase()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            stat.AddModifier(new StatModifier("buff", StatModifierType.Percent, 0.3f));
            Assert.AreEqual(130f, stat.CurrentValue, 0.01f);
        }

        [Test]
        public void Additive_MixedModifiers()
        {
            // final = 100 * (1 + 0.3) + 20 = 150
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            stat.AddModifier(new StatModifier("buff", StatModifierType.Percent, 0.3f));
            stat.AddModifier(new StatModifier("item", StatModifierType.Flat, 20f));
            Assert.AreEqual(150f, stat.CurrentValue, 0.01f);
        }

        [Test]
        public void Additive_MultiplePercentModifiers()
        {
            // final = 100 * (1 + 0.3 + 0.2) = 150
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            stat.AddModifier(new StatModifier("a", StatModifierType.Percent, 0.3f));
            stat.AddModifier(new StatModifier("b", StatModifierType.Percent, 0.2f));
            Assert.AreEqual(150f, stat.CurrentValue, 0.01f);
        }

        // ── SeparatedMultiplicative 계산 ──

        [Test]
        public void SeparatedMult_SingleIncrease()
        {
            // base=0, +30% → 0 + 0.3 = 0.3
            var stat = new UnitStat(StatCalculationMode.SeparatedMultiplicative);
            stat.SetBaseValue(0f);
            stat.AddModifier(new StatModifier("armor", StatModifierType.Flat, 0.3f));
            Assert.AreEqual(0.3f, stat.CurrentValue, 0.001f);
        }

        [Test]
        public void SeparatedMult_MultipleIncreases()
        {
            // base=0, +30%, +20% → 1 - (1-0.3)(1-0.2) = 0.44
            var stat = new UnitStat(StatCalculationMode.SeparatedMultiplicative);
            stat.SetBaseValue(0f);
            stat.AddModifier(new StatModifier("a", StatModifierType.Flat, 0.3f));
            stat.AddModifier(new StatModifier("b", StatModifierType.Flat, 0.2f));
            Assert.AreEqual(0.44f, stat.CurrentValue, 0.001f);
        }

        [Test]
        public void SeparatedMult_IncreaseAndDecrease()
        {
            // 기획서 예시: base=0, +30%, +20%, -10%
            // 증가: 1 - (1-0.3)(1-0.2) = 0.44
            // 감소: 1 - (1-0.1) = 0.10
            // 최종: 0 + 0.44 - 0.10 = 0.34
            var stat = new UnitStat(StatCalculationMode.SeparatedMultiplicative);
            stat.SetBaseValue(0f);
            stat.AddModifier(new StatModifier("a", StatModifierType.Flat, 0.3f));
            stat.AddModifier(new StatModifier("b", StatModifierType.Flat, 0.2f));
            stat.AddModifier(new StatModifier("debuff", StatModifierType.Flat, -0.1f));
            Assert.AreEqual(0.34f, stat.CurrentValue, 0.001f);
        }

        [Test]
        public void SeparatedMult_WithBaseValue()
        {
            // base=0.2, +30% → 0.2 + 0.3 = 0.5
            var stat = new UnitStat(StatCalculationMode.SeparatedMultiplicative);
            stat.SetBaseValue(0.2f);
            stat.AddModifier(new StatModifier("a", StatModifierType.Flat, 0.3f));
            Assert.AreEqual(0.5f, stat.CurrentValue, 0.001f);
        }

        // ── 수정자 추가/제거 ──

        [Test]
        public void RemoveModifier_RecalculatesCorrectly()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            var mod = new StatModifier("buff", StatModifierType.Flat, 50f);
            stat.AddModifier(mod);
            Assert.AreEqual(150f, stat.CurrentValue);

            stat.RemoveModifier(mod);
            Assert.AreEqual(100f, stat.CurrentValue);
        }

        [Test]
        public void RemoveModifiersBySource_RemovesOnlyMatchingSource()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            stat.AddModifier(new StatModifier("synergy", StatModifierType.Flat, 20f));
            stat.AddModifier(new StatModifier("buff", StatModifierType.Flat, 30f));
            stat.AddModifier(new StatModifier("synergy", StatModifierType.Percent, 0.1f));

            stat.RemoveModifiersBySource("synergy");
            // buff만 남음: 100 * 1 + 30 = 130
            Assert.AreEqual(130f, stat.CurrentValue, 0.01f);
        }

        [Test]
        public void ClearModifiers_ResetsToBase()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);
            stat.AddModifier(new StatModifier("a", StatModifierType.Flat, 50f));
            stat.AddModifier(new StatModifier("b", StatModifierType.Percent, 0.5f));
            stat.ClearModifiers();
            Assert.AreEqual(100f, stat.CurrentValue);
        }

        // ── OnChange 이벤트 ──

        [Test]
        public void OnChange_FiresOnValueChange()
        {
            var stat = new UnitStat();
            float receivedValue = 0f;
            stat.OnChange += v => receivedValue = v;

            stat.SetBaseValue(100f);
            Assert.AreEqual(100f, receivedValue);
        }

        [Test]
        public void OnChange_FiresOnModifierChange()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);

            float receivedValue = 0f;
            stat.OnChange += v => receivedValue = v;

            stat.AddModifier(new StatModifier("buff", StatModifierType.Flat, 25f));
            Assert.AreEqual(125f, receivedValue);
        }

        [Test]
        public void OnChange_DoesNotFireWhenValueUnchanged()
        {
            var stat = new UnitStat();
            stat.SetBaseValue(100f);

            int callCount = 0;
            stat.OnChange += _ => callCount++;

            stat.SetBaseValue(100f);
            Assert.AreEqual(0, callCount);
        }
    }
}
