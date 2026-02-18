using System.Collections.Generic;
using Common.Data.Units.UnitStatsByLevel;
using NUnit.Framework;
using Scenes.Battle.Feature.Units.UnitStats;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public class UnitStatSheetTests
    {
        private UnitStatSheet _sheet;
        private UnitStatsByLevelData _data;

        // 테스트용 스탯 기대값 (star 1)
        private static readonly Dictionary<string, float> TestValues = new()
        {
            { "maxHealth", 100f },
            { "physicalAttack", 50f },
            { "magicAttack", 40f },
            { "physicalDefense", 0.2f },
            { "magicDefense", 0.15f },
            { "attackSpeed", 1.5f },
            { "attackRange", 3f },
            { "moveSpeed", 2f },
            { "criticalChance", 0.1f },
            { "criticalDamageMultiplier", 1.5f },
            { "cooldownReduction", 0f },
            { "statusResistance", 0f },
            { "damageDealtIncrease", 0f },
            { "damageReduction", 0f },
        };

        [SetUp]
        public void SetUp()
        {
            _sheet = new UnitStatSheet();
            _data = ScriptableObject.CreateInstance<UnitStatsByLevelData>();

            var so = new SerializedObject(_data);
            foreach (var (field, value) in TestValues)
            {
                var list = so.FindProperty(field).FindPropertyRelative("baseValuesByStar");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).floatValue = value;
            }
            so.ApplyModifiedProperties();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        // ── Init 검증 ──

        [Test]
        public void Init_SetsAllStatsFromData()
        {
            _sheet.Init(_data);

            Assert.AreEqual(100f, _sheet.MaxHealth.CurrentValue, 0.01f);
            Assert.AreEqual(50f, _sheet.PhysicalAttack.CurrentValue, 0.01f);
            Assert.AreEqual(40f, _sheet.MagicAttack.CurrentValue, 0.01f);
            Assert.AreEqual(0.2f, _sheet.PhysicalDefense.CurrentValue, 0.01f);
            Assert.AreEqual(0.15f, _sheet.MagicDefense.CurrentValue, 0.01f);
            Assert.AreEqual(1.5f, _sheet.AttackSpeed.CurrentValue, 0.01f);
            Assert.AreEqual(3f, _sheet.AttackRange.CurrentValue, 0.01f);
            Assert.AreEqual(2f, _sheet.MoveSpeed.CurrentValue, 0.01f);
            Assert.AreEqual(0.1f, _sheet.CriticalChance.CurrentValue, 0.01f);
            Assert.AreEqual(1.5f, _sheet.CriticalDamageMultiplier.CurrentValue, 0.01f);
            Assert.AreEqual(0f, _sheet.CooldownReduction.CurrentValue, 0.01f);
            Assert.AreEqual(0f, _sheet.StatusResistance.CurrentValue, 0.01f);
            Assert.AreEqual(0f, _sheet.DamageDealtIncrease.CurrentValue, 0.01f);
            Assert.AreEqual(0f, _sheet.DamageReduction.CurrentValue, 0.01f);
        }

        [Test]
        public void Init_SetsHealthToMaxHealth()
        {
            _sheet.Init(_data);

            Assert.AreEqual(_sheet.MaxHealth.CurrentValue, _sheet.Health, 0.01f);
        }

        [Test]
        public void Init_ClearsExistingModifiers()
        {
            _sheet.PhysicalAttack.AddModifier(
                new StatModifier("buff", StatModifierType.Flat, 999f));
            Assert.AreEqual(999f, _sheet.PhysicalAttack.CurrentValue, 0.01f);

            _sheet.Init(_data);

            Assert.AreEqual(50f, _sheet.PhysicalAttack.CurrentValue, 0.01f);
        }

        // ── Health 검증 ──

        [Test]
        public void Health_ClampsToZeroAndMax()
        {
            _sheet.Init(_data); // MaxHealth = 100

            _sheet.Health = 999f;
            Assert.AreEqual(100f, _sheet.Health, 0.01f);

            _sheet.Health = -50f;
            Assert.AreEqual(0f, _sheet.Health, 0.01f);
        }

        [Test]
        public void Health_FiresOnHealthChangeEvent()
        {
            _sheet.Init(_data); // Health = 100

            float received = -1f;
            _sheet.OnHealthChange += v => received = v;

            _sheet.Health = 60f;
            Assert.AreEqual(60f, received, 0.01f);
        }
    }
}
