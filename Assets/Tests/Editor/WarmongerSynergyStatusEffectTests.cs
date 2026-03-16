using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.SerializableDictionary;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyEffects;
using Scenes.Battle.Feature.Units;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// WarmongerSynergyStatusEffect(전쟁기계)의 조건부 DamageReduction 적용/갱신/해제를 검증한다.
    /// </summary>
    public class WarmongerSynergyStatusEffectTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private Unit _unit;
        private GameObject _gameObject;

        private const float Tier1LowReduction = 0.18f;
        private const float Tier1HighReduction = 0.25f;
        private const float Tier2LowReduction = 0.20f;
        private const float Tier2HighReduction = 0.30f;
        private const float HealPercent = 0.05f;
        private const float BaseMaxHealth = 100f;
        private const float BaseDamageReduction = 0f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();

            var constants1 = new SerializableDictionary<string, float>();
            constants1["lowDamageReduction"] = Tier1LowReduction;
            constants1["highDamageReduction"] = Tier1HighReduction;
            constants1["healPercent"] = HealPercent;
            var constants2 = new SerializableDictionary<string, float>();
            constants2["lowDamageReduction"] = Tier2LowReduction;
            constants2["highDamageReduction"] = Tier2HighReduction;
            constants2["healPercent"] = HealPercent;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
                CreateTier(2, 4, constants2),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            _gameObject = new GameObject("TestUnit");
            _unit = _gameObject.AddComponent<StubUnit>();
            _unit.StatSheet.MaxHealth.SetBaseValue(BaseMaxHealth);
            _unit.StatSheet.DamageReduction.SetBaseValue(BaseDamageReduction);
            _unit.StatSheet.SetCurrentHealth(BaseMaxHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_definition);
        }

        // ── 체력 50% 이상에서 highDamageReduction 적용 ──

        [Test]
        public void OnSynergyActivated_FullHealth_AppliesHighReduction()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            Assert.AreEqual(Tier1HighReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 체력 50% 미만에서 lowDamageReduction 적용 ──

        [Test]
        public void OnSynergyActivated_LowHealth_AppliesLowReduction()
        {
            _unit.StatSheet.SetCurrentHealth(BaseMaxHealth * 0.3f);
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            Assert.AreEqual(Tier1LowReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 체력 변화로 임계값 하향 교차 시 수정자 전환 ──

        [Test]
        public void OnHealthChanged_CrossesBelow50Percent_SwitchesToLowReduction()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            _unit.StatSheet.SetCurrentHealth(BaseMaxHealth * 0.4f);

            Assert.AreEqual(Tier1LowReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 체력 변화로 임계값 상향 교차 시 수정자 전환 ──

        [Test]
        public void OnHealthChanged_CrossesAbove50Percent_SwitchesToHighReduction()
        {
            _unit.StatSheet.SetCurrentHealth(BaseMaxHealth * 0.3f);
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            _unit.StatSheet.SetCurrentHealth(BaseMaxHealth * 0.6f);

            Assert.AreEqual(Tier1HighReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 임계값 내 체력 변화 시 수정자 유지 ──

        [Test]
        public void OnHealthChanged_StaysAbove50Percent_KeepsHighReduction()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            _unit.StatSheet.SetCurrentHealth(BaseMaxHealth * 0.7f);

            Assert.AreEqual(Tier1HighReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 티어 변경 시 수정자 값 갱신 ──

        [Test]
        public void OnSynergyTierChanged_UpdatesModifierValue()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            _activation.Recalculate(4);

            Assert.AreEqual(Tier2HighReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 비활성화 시 수정자 제거 ──

        [Test]
        public void OnSynergyDeactivated_RemovesModifier()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            _activation.Recalculate(0);

            Assert.AreEqual(BaseDamageReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── OnRemove 시 수정자 제거 (안전장치) ──

        [Test]
        public void OnRemove_RemovesModifier()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            effect.OnRemove();

            Assert.AreEqual(BaseDamageReduction, _unit.StatSheet.DamageReduction.CurrentValue, 0.01f);
        }

        // ── 헬퍼 ──

        /// <summary>WarmongerSSE를 생성하고 Context와 함께 Apply한다. DefenderManager는 null (사망 회복 미테스트).</summary>
        private WarmongerSynergyStatusEffect CreateAndApplyEffect()
        {
            var effect = new WarmongerSynergyStatusEffect(null, null);
            var context = new SynergyStatusEffectContext(_activation, _definition, _unit);
            effect.OnApply(context);
            return effect;
        }

        /// <summary>테스트용 Unit stub. Awake의 healthBar 구독을 방지한다.</summary>
        private class StubUnit : Unit
        {
            protected override void Awake() { }
        }

        /// <summary>리플렉션으로 SynergyTier를 생성한다.</summary>
        private static SynergyTier CreateTier(int tierLevel, int requiredCount,
            SerializableDictionary<string, float> constants)
        {
            var tier = new SynergyTier();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            object boxed = tier;
            typeof(SynergyTier).GetField("tier", flags).SetValue(boxed, tierLevel);
            typeof(SynergyTier).GetField("requiredCount", flags).SetValue(boxed, requiredCount);
            typeof(SynergyTier).GetField("constants", flags).SetValue(boxed, constants);
            return (SynergyTier)boxed;
        }

        /// <summary>리플렉션으로 SynergyDefinitionData의 tiers 필드를 설정한다.</summary>
        private static void SetTiers(SynergyDefinitionData definition, List<SynergyTier> tiers)
        {
            var tiersField = typeof(SynergyDefinitionData)
                .GetField("tiers", BindingFlags.NonPublic | BindingFlags.Instance);
            tiersField.SetValue(definition, tiers);
        }
    }
}
