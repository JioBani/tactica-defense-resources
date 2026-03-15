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
    /// ArcanistSynergyStatusEffect(비전 마법사 SSE)의 MagicAttack Flat 수정자 적용/갱신/해제를 검증한다.
    /// </summary>
    public class ArcanistSynergyStatusEffectTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private Unit _unit;
        private GameObject _gameObject;

        private const float Tier1ArcanistSpellPower = 25f;
        private const float Tier2ArcanistSpellPower = 50f;
        private const float BaseMagicAttack = 10f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            var constants1 = new SerializableDictionary<string, float>();
            constants1["arcanistSpellPower"] = Tier1ArcanistSpellPower;
            var constants2 = new SerializableDictionary<string, float>();
            constants2["arcanistSpellPower"] = Tier2ArcanistSpellPower;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
                CreateTier(2, 4, constants2),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            _gameObject = new GameObject("TestUnit");
            _unit = _gameObject.AddComponent<StubUnit>();
            _unit.StatSheet.MagicAttack.SetBaseValue(BaseMagicAttack);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void OnSynergyActivated_AddsFlatModifierToMagicAttack()
        {
            _activation.Recalculate(2);
            var effect = new ArcanistSynergyStatusEffect(null);
            ApplyEffect(effect);

            float expected = BaseMagicAttack + Tier1ArcanistSpellPower;
            Assert.AreEqual(expected, _unit.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        [Test]
        public void OnSynergyTierChanged_UpdatesModifierValue()
        {
            _activation.Recalculate(2);
            var effect = new ArcanistSynergyStatusEffect(null);
            ApplyEffect(effect);

            _activation.Recalculate(4);

            float expected = BaseMagicAttack + Tier2ArcanistSpellPower;
            Assert.AreEqual(expected, _unit.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        [Test]
        public void OnSynergyDeactivated_RemovesModifier()
        {
            _activation.Recalculate(2);
            var effect = new ArcanistSynergyStatusEffect(null);
            ApplyEffect(effect);

            _activation.Recalculate(0);

            Assert.AreEqual(BaseMagicAttack, _unit.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        [Test]
        public void OnRemove_RemovesModifier()
        {
            _activation.Recalculate(2);
            var effect = new ArcanistSynergyStatusEffect(null);
            ApplyEffect(effect);

            effect.OnRemove();

            Assert.AreEqual(BaseMagicAttack, _unit.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        [Test]
        public void OnApply_WhenInactive_DoesNotAddModifier()
        {
            var effect = new ArcanistSynergyStatusEffect(null);
            ApplyEffect(effect);

            Assert.AreEqual(BaseMagicAttack, _unit.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        private void ApplyEffect(ArcanistSynergyStatusEffect effect)
        {
            var context = new SynergyStatusEffectContext(_activation, _definition, _unit);
            effect.OnApply(context);
        }

        private class StubUnit : Unit { }

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

        private static void SetTiers(SynergyDefinitionData definition, List<SynergyTier> tiers)
        {
            var tiersField = typeof(SynergyDefinitionData)
                .GetField("tiers", BindingFlags.NonPublic | BindingFlags.Instance);
            tiersField.SetValue(definition, tiers);
        }
    }
}