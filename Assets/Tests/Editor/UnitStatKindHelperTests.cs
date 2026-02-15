using Common.Data.Units.UnitStatsByLevel;
using NUnit.Framework;
using Scenes.Battle.Feature.Ui.StatInfoPanel;

namespace Tests.Editor
{
    public class UnitStatKindHelperTests
    {
        // ── GetDisplayName ──

        [Test]
        public void GetDisplayName_MaxHealth_Returns체력()
        {
            Assert.AreEqual("체력", UnitStatKindHelper.GetDisplayName(UnitStatKind.MaxHealth));
        }

        [Test]
        public void GetDisplayName_PhysicalAttack_Returns물리공격력()
        {
            Assert.AreEqual("물리공격력", UnitStatKindHelper.GetDisplayName(UnitStatKind.PhysicalAttack));
        }

        [Test]
        public void GetDisplayName_MagicAttack_Returns마법공격력()
        {
            Assert.AreEqual("마법공격력", UnitStatKindHelper.GetDisplayName(UnitStatKind.MagicAttack));
        }

        [Test]
        public void GetDisplayName_PhysicalDefense_Returns물리방어력()
        {
            Assert.AreEqual("물리방어력", UnitStatKindHelper.GetDisplayName(UnitStatKind.PhysicalDefense));
        }

        [Test]
        public void GetDisplayName_MagicDefense_Returns마법방어력()
        {
            Assert.AreEqual("마법방어력", UnitStatKindHelper.GetDisplayName(UnitStatKind.MagicDefense));
        }

        [Test]
        public void GetDisplayName_AttackSpeed_Returns공격속도()
        {
            Assert.AreEqual("공격속도", UnitStatKindHelper.GetDisplayName(UnitStatKind.AttackSpeed));
        }

        [Test]
        public void GetDisplayName_AttackRange_Returns사거리()
        {
            Assert.AreEqual("사거리", UnitStatKindHelper.GetDisplayName(UnitStatKind.AttackRange));
        }

        [Test]
        public void GetDisplayName_MoveSpeed_Returns이동속도()
        {
            Assert.AreEqual("이동속도", UnitStatKindHelper.GetDisplayName(UnitStatKind.MoveSpeed));
        }

        [Test]
        public void GetDisplayName_CriticalChance_Returns치명타확률()
        {
            Assert.AreEqual("치명타 확률", UnitStatKindHelper.GetDisplayName(UnitStatKind.CriticalChance));
        }

        [Test]
        public void GetDisplayName_CriticalDamageMultiplier_Returns치명타피해배수()
        {
            Assert.AreEqual("치명타 피해 배수", UnitStatKindHelper.GetDisplayName(UnitStatKind.CriticalDamageMultiplier));
        }

        [Test]
        public void GetDisplayName_CooldownReduction_Returns스킬쿨타임감소()
        {
            Assert.AreEqual("스킬 쿨타임 감소", UnitStatKindHelper.GetDisplayName(UnitStatKind.CooldownReduction));
        }

        [Test]
        public void GetDisplayName_StatusResistance_Returns상태저항력()
        {
            Assert.AreEqual("상태저항력", UnitStatKindHelper.GetDisplayName(UnitStatKind.StatusResistance));
        }

        [Test]
        public void GetDisplayName_DamageDealtIncrease_Returns입히는피해증가()
        {
            Assert.AreEqual("입히는 피해 증가", UnitStatKindHelper.GetDisplayName(UnitStatKind.DamageDealtIncrease));
        }

        [Test]
        public void GetDisplayName_DamageReduction_Returns받는피해감소()
        {
            Assert.AreEqual("받는 피해 감소", UnitStatKindHelper.GetDisplayName(UnitStatKind.DamageReduction));
        }

        // ── FormatStatValue: 퍼센트 표시 스탯 ──

        [Test]
        public void FormatStatValue_CriticalChance_FormatsAsPercent()
        {
            Assert.AreEqual("25%", UnitStatKindHelper.FormatStatValue(UnitStatKind.CriticalChance, 0.25f));
        }

        [Test]
        public void FormatStatValue_CooldownReduction_FormatsAsPercent()
        {
            Assert.AreEqual("10%", UnitStatKindHelper.FormatStatValue(UnitStatKind.CooldownReduction, 0.1f));
        }

        [Test]
        public void FormatStatValue_StatusResistance_FormatsAsPercent()
        {
            Assert.AreEqual("30%", UnitStatKindHelper.FormatStatValue(UnitStatKind.StatusResistance, 0.3f));
        }

        [Test]
        public void FormatStatValue_DamageDealtIncrease_FormatsAsPercent()
        {
            Assert.AreEqual("15%", UnitStatKindHelper.FormatStatValue(UnitStatKind.DamageDealtIncrease, 0.15f));
        }

        [Test]
        public void FormatStatValue_DamageReduction_FormatsAsPercent()
        {
            Assert.AreEqual("20%", UnitStatKindHelper.FormatStatValue(UnitStatKind.DamageReduction, 0.2f));
        }

        [Test]
        public void FormatStatValue_PercentZero_Shows0Percent()
        {
            Assert.AreEqual("0%", UnitStatKindHelper.FormatStatValue(UnitStatKind.CriticalChance, 0f));
        }

        [Test]
        public void FormatStatValue_PercentFull_Shows100Percent()
        {
            Assert.AreEqual("100%", UnitStatKindHelper.FormatStatValue(UnitStatKind.DamageReduction, 1f));
        }

        // ── FormatStatValue: 소수점 2자리 스탯 (공격속도) ──

        [Test]
        public void FormatStatValue_AttackSpeed_FormatsTwoDecimals()
        {
            Assert.AreEqual("1.50", UnitStatKindHelper.FormatStatValue(UnitStatKind.AttackSpeed, 1.5f));
        }

        [Test]
        public void FormatStatValue_AttackSpeed_SmallValue()
        {
            Assert.AreEqual("0.80", UnitStatKindHelper.FormatStatValue(UnitStatKind.AttackSpeed, 0.8f));
        }

        // ── FormatStatValue: 정수 표시 스탯 ──

        [Test]
        public void FormatStatValue_MaxHealth_FormatsAsInteger()
        {
            Assert.AreEqual("1000", UnitStatKindHelper.FormatStatValue(UnitStatKind.MaxHealth, 1000f));
        }

        [Test]
        public void FormatStatValue_PhysicalAttack_FormatsAsInteger()
        {
            Assert.AreEqual("150", UnitStatKindHelper.FormatStatValue(UnitStatKind.PhysicalAttack, 150f));
        }

        [Test]
        public void FormatStatValue_MoveSpeed_FormatsAsInteger()
        {
            Assert.AreEqual("3", UnitStatKindHelper.FormatStatValue(UnitStatKind.MoveSpeed, 3.2f));
        }

        [Test]
        public void FormatStatValue_AttackRange_FormatsAsInteger()
        {
            Assert.AreEqual("5", UnitStatKindHelper.FormatStatValue(UnitStatKind.AttackRange, 5f));
        }
    }
}
