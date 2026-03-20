using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Data.Units.UnitDefinitions;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SerializableDictionary;
using Common.Scripts.StatusEffect;
using NUnit.Framework;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyControllers;
using Scenes.Battle.Feature.Unit.Defenders;
using Scenes.Battle.Feature.Units;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// SynergyController 기본 동작을 검증한다.
    /// BruiserSynergyController(단순형)로 진입/퇴장/판매 시나리오를 테스트한다.
    /// </summary>
    public class SynergyControllerTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private BruiserSynergyController _controller;

        private Defender _defender1;
        private Defender _defender2;
        private GameObject _go1;
        private GameObject _go2;

        private readonly List<ScriptableObject> _createdAssets = new();

        private const float Tier1HealthPercent = 0.25f;
        private const float BaseMaxHealth = 100f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(_definition);

            var constants1 = new SerializableDictionary<string, float>();
            constants1["healthPercent"] = Tier1HealthPercent;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            _go1 = new GameObject("BruiserUnit1");
            _defender1 = CreateDefenderWithSynergy(_go1, _definition, unitId: 1);
            _defender1.StatSheet.MaxHealth.SetBaseValue(BaseMaxHealth);

            _go2 = new GameObject("BruiserUnit2");
            _defender2 = CreateDefenderWithSynergy(_go2, _definition, unitId: 2);
            _defender2.StatSheet.MaxHealth.SetBaseValue(BaseMaxHealth);

            _controller = new BruiserSynergyController(_activation);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            Object.DestroyImmediate(_go1);
            Object.DestroyImmediate(_go2);

            foreach (ScriptableObject asset in _createdAssets)
            {
                Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        // ── 시너지 활성화: 2유닛 진입 시 SSE가 부여된다 ──

        [Test]
        public void TwoDefendersEnter_TierActivates_SSEApplied()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender2, Placement.BattleArea);

            float expected = BaseMaxHealth * (1f + Tier1HealthPercent);
            Assert.AreEqual(expected, _defender1.StatSheet.MaxHealth.CurrentValue, 0.01f);
            Assert.AreEqual(expected, _defender2.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 시너지 활성 상태에서 유닛 퇴장 시 SSE가 해제된다 ──

        [Test]
        public void DefenderLeavesWhileActive_SSERemoved_StatRestored()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender2, Placement.BattleArea);

            PublishPlacement(_defender1, Placement.WaitingArea);

            Assert.AreEqual(BaseMaxHealth, _defender1.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 판매(Despawn) 시 SSE가 해제된다 ──

        [Test]
        public void DefenderDespawned_SSERemoved_StatRestored()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender2, Placement.BattleArea);

            GlobalEventBus.Publish(new OnDefenderChangedEventDto(_defender1, DefenderChanges.Despawn));

            Assert.AreEqual(BaseMaxHealth, _defender1.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 시너지 활성 상태에서 새 유닛 진입 시 SSE가 부여된다 ──

        [Test]
        public void NewDefenderEntersWhileActive_SSEApplied()
        {
            // 3번째 Defender 생성
            var go3 = new GameObject("BruiserUnit3");
            Defender defender3 = CreateDefenderWithSynergy(go3, _definition, unitId: 3);
            defender3.StatSheet.MaxHealth.SetBaseValue(BaseMaxHealth);

            // 2유닛으로 티어 활성화
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender2, Placement.BattleArea);

            // 시너지 활성 상태에서 3번째 유닛 진입
            PublishPlacement(defender3, Placement.BattleArea);

            float expected = BaseMaxHealth * (1f + Tier1HealthPercent);
            Assert.AreEqual(expected, defender3.StatSheet.MaxHealth.CurrentValue, 0.01f);

            Object.DestroyImmediate(go3);
        }

        // ── 퇴장 후 재진입 시 SSE가 다시 부여된다 ──

        [Test]
        public void DefenderLeavesAndReenters_SSEReapplied()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender2, Placement.BattleArea);

            // 퇴장
            PublishPlacement(_defender1, Placement.WaitingArea);
            Assert.AreEqual(BaseMaxHealth, _defender1.StatSheet.MaxHealth.CurrentValue, 0.01f);

            // 재진입
            PublishPlacement(_defender1, Placement.BattleArea);

            float expected = BaseMaxHealth * (1f + Tier1HealthPercent);
            Assert.AreEqual(expected, _defender1.StatSheet.MaxHealth.CurrentValue, 0.01f);
        }

        // ── 헬퍼 ──

        private static void PublishPlacement(Defender defender, Placement placement)
        {
            GlobalEventBus.Publish(new OnDefenderPlacementChangedEventDto(defender, placement));
        }

        /// <summary>Defender에 StatusEffectController와 UnitLoadOutData를 설정한다.</summary>
        private Defender CreateDefenderWithSynergy(GameObject go, SynergyDefinitionData synergy, int unitId)
        {
            var sec = go.AddComponent<StatusEffectController>();
            var defender = go.AddComponent<StubDefender>();

            var secField = typeof(Unit)
                .GetField("statusEffectController", BindingFlags.NonPublic | BindingFlags.Instance);
            secField.SetValue(defender, sec);

            var unitDef = ScriptableObject.CreateInstance<UnitDefinitionData>();
            _createdAssets.Add(unitDef);
            SetPrivateField(unitDef, "id", unitId);
            if (synergy != null)
            {
                SetPrivateField(unitDef, "summonerEffect", synergy);
            }

            var loadOut = ScriptableObject.CreateInstance<UnitLoadOutData>();
            _createdAssets.Add(loadOut);
            SetPrivateField(loadOut, "unit", unitDef);

            defender.UnitLoadOutData = loadOut;

            return defender;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }

        /// <summary>테스트용 Defender stub.</summary>
        private class StubDefender : Defender { }

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
