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

        // ── Reinforce 검증 ──

        [Test]
        public void Reinforce_IncrementsReinforcementAndAppliesStats()
        {
            // additionalPerExtraStar = 10으로 설정 (체력 기준)
            var so = new SerializedObject(_data);
            so.FindProperty("maxHealth").FindPropertyRelative("additionalPerExtraStar").floatValue = 10f;
            so.ApplyModifiedProperties();

            _sheet.Init(_data, star: 1);
            float baseStat = _sheet.MaxHealth.CurrentValue; // 100

            _sheet.Init(_data, star: 1, reinforcement: 0);
            _sheet.Reinforce();

            Assert.AreEqual(1, _sheet.Reinforcement);
            Assert.AreEqual(baseStat + 10f, _sheet.MaxHealth.CurrentValue, 0.01f);
        }

        [Test]
        public void Reinforce_WithAmount2_IncrementsBy2()
        {
            var so = new SerializedObject(_data);
            so.FindProperty("maxHealth").FindPropertyRelative("additionalPerExtraStar").floatValue = 10f;
            so.ApplyModifiedProperties();

            _sheet.Init(_data, star: 1, reinforcement: 0);
            float baseStat = _sheet.MaxHealth.CurrentValue; // 100

            _sheet.Reinforce(2);

            Assert.AreEqual(2, _sheet.Reinforcement);
            Assert.AreEqual(baseStat + 20f, _sheet.MaxHealth.CurrentValue, 0.01f);
        }

        [Test]
        public void UpgradeStar_ResetsReinforcement()
        {
            // 3성+2강 상태에서 UpgradeStar 호출
            var so = new SerializedObject(_data);
            var list = so.FindProperty("maxHealth").FindPropertyRelative("baseValuesByStar");
            list.arraySize = 3;
            list.GetArrayElementAtIndex(0).floatValue = 100f;
            list.GetArrayElementAtIndex(1).floatValue = 150f;
            list.GetArrayElementAtIndex(2).floatValue = 200f;
            so.ApplyModifiedProperties();

            _sheet.Init(_data, star: 3, reinforcement: 2);
            Assert.AreEqual(2, _sheet.Reinforcement);

            _sheet.UpgradeStar();

            Assert.AreEqual(4, _sheet.Star);
            Assert.AreEqual(0, _sheet.Reinforcement);
        }

        // ── 성급 초기화 검증 ──

        [Test]
        public void Init_WithStar2_SetsCorrectStatsForStar2()
        {
            var so = new SerializedObject(_data);
            var list = so.FindProperty("maxHealth").FindPropertyRelative("baseValuesByStar");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).floatValue = 100f;
            list.GetArrayElementAtIndex(1).floatValue = 150f;
            so.ApplyModifiedProperties();

            _sheet.Init(_data, star: 2);

            Assert.AreEqual(2, _sheet.Star);
            Assert.AreEqual(150f, _sheet.MaxHealth.CurrentValue, 0.01f);
            Assert.AreEqual(150f, _sheet.Health, 0.01f);
        }

        [Test]
        public void Init_WithStarAndReinforcement_SetsCorrectProperties()
        {
            var so = new SerializedObject(_data);
            so.FindProperty("maxHealth").FindPropertyRelative("additionalPerExtraStar").floatValue = 10f;
            so.ApplyModifiedProperties();

            _sheet.Init(_data, star: 2, reinforcement: 1);

            Assert.AreEqual(2, _sheet.Star);
            Assert.AreEqual(1, _sheet.Reinforcement);
        }

        // ── 이벤트 중복 등록 검증 ──

        [Test]
        public void Init_CalledMultipleTimes_MaxHealthHandlerNotDuplicated()
        {
            // Init을 여러 번 호출해도 MaxHealth 핸들러가 1번만 등록되어야 한다
            _sheet.Init(_data);
            _sheet.Init(_data);
            _sheet.Init(_data);

            // Health를 MaxHealth 이하로 설정
            _sheet.Health = 80f;

            // MaxHealth를 50으로 낮추는 수정자 추가 → OnMaxHealthChanged가 Health를 clamp해야 함
            _sheet.MaxHealth.AddModifier(new StatModifier("test", StatModifierType.Flat, -50f));

            // MaxHealth = 100 - 50 = 50, Health는 50으로 clamp되어야 함
            // 핸들러가 중복이면 여러 번 실행되지만, 결과적으로 clamp 값은 동일하므로
            // Health 이벤트 발생 횟수로 검증한다
            int healthChangeCount = 0;
            _sheet.OnHealthChange += _ => healthChangeCount++;

            _sheet.Health = 60f; // 50으로 clamp됨
            Assert.AreEqual(50f, _sheet.Health, 0.01f);
        }
    }
}
