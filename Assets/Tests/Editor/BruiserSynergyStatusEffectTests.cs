using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.SerializableDictionary;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyEffects;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// BruiserSynergyStatusEffect(난동꾼)의 스탯 수정자 적용/갱신/해제를 검증한다.
    /// </summary>
    public class BruiserSynergyStatusEffectTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private Unit _unit;
        private GameObject _gameObject;

        private const float Tier1HealthPercent = 0.25f;
        private const float Tier2HealthPercent = 0.40f;
        private const float BaseMaxHealth = 100f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            var constants1 = new SerializableDictionary<string, float>();
            constants1["healthPercent"] = Tier1HealthPercent;
            var constants2 = new SerializableDictionary<string, float>();
            constants2["healthPercent"] = Tier2HealthPercent;

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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_definition);
        }

        // ── 활성화 시 MaxHealth에 % 수정자 적용 ──

        [Test]
        public void OnSynergyActivated_AddsPercentModifierToMaxHealth()
        {
            _activation.Recalculate(2); // 1티어 활성화
            var effect = new BruiserSynergyStatusEffect(null);
            ApplyEffect(effect);

            float expected = BaseMaxHealth * (1f + Tier1HealthPercent);
            Assert.AreEqual(expected, _unit.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 티어 변경 시 수정자 값 갱신 ──

        [Test]
        public void OnSynergyTierChanged_UpdatesModifierValue()
        {
            _activation.Recalculate(2); // 1티어
            var effect = new BruiserSynergyStatusEffect(null);
            ApplyEffect(effect);

            _activation.Recalculate(4); // 2티어로 변경

            float expected = BaseMaxHealth * (1f + Tier2HealthPercent);
            Assert.AreEqual(expected, _unit.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 비활성화 시 수정자 제거 ──

        [Test]
        public void OnSynergyDeactivated_RemovesModifier()
        {
            _activation.Recalculate(2); // 1티어
            var effect = new BruiserSynergyStatusEffect(null);
            ApplyEffect(effect);

            _activation.Recalculate(0); // 비활성화

            Assert.AreEqual(BaseMaxHealth, _unit.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── OnRemove 시 수정자 제거 (안전장치) ──

        [Test]
        public void OnRemove_RemovesModifier()
        {
            _activation.Recalculate(2); // 1티어
            var effect = new BruiserSynergyStatusEffect(null);
            ApplyEffect(effect);

            effect.OnRemove();

            Assert.AreEqual(BaseMaxHealth, _unit.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 비활성 상태에서 Apply 시 수정자 미적용 ──

        [Test]
        public void OnApply_WhenInactive_DoesNotAddModifier()
        {
            var effect = new BruiserSynergyStatusEffect(null);
            ApplyEffect(effect);

            Assert.AreEqual(BaseMaxHealth, _unit.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        /// <summary>SSE를 Context와 함께 Apply한다.</summary>
        private void ApplyEffect(BruiserSynergyStatusEffect effect)
        {
            var context = new SynergyStatusEffectContext(_activation, _definition, _unit);
            effect.OnApply(context);
        }

        /// <summary>테스트용 Unit stub. Awake 호출 없이 StatSheet만 사용한다.</summary>
        private class StubUnit : Unit { }

        /// <summary>리플렉션으로 SynergyTier를 생성한다.</summary>
        private static SynergyTier CreateTier(int tierLevel, int requiredCount, SerializableDictionary<string, float> constants)
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
