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
    /// ArcanistSynergyController의 대상 분기 로직을 검증한다.
    /// 시너지 보유 Defender에는 ArcanistSSE를, 비보유 Defender에는 ArcanistSpellPowerEffect를 부여한다.
    /// </summary>
    public class ArcanistSynergyControllerTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private ArcanistSynergyController _controller;

        private Defender _arcanistDefender;
        private Defender _nonArcanistDefender;
        private GameObject _arcanistGo;
        private GameObject _nonArcanistGo;

        private readonly List<ScriptableObject> _createdAssets = new();

        private const float Tier1SpellPower = 15f;
        private const float Tier1ArcanistSpellPower = 25f;
        private const float BaseMagicAttack = 10f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(_definition);

            var constants1 = new SerializableDictionary<string, float>();
            constants1["spellPower"] = Tier1SpellPower;
            constants1["arcanistSpellPower"] = Tier1ArcanistSpellPower;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 1, constants1),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            _arcanistGo = new GameObject("ArcanistUnit");
            _arcanistDefender = CreateDefenderWithSynergy(_arcanistGo, _definition, unitId: 1);
            _arcanistDefender.StatSheet.MagicAttack.SetBaseValue(BaseMagicAttack);

            _nonArcanistGo = new GameObject("NonArcanistUnit");
            _nonArcanistDefender = CreateDefenderWithSynergy(_nonArcanistGo, synergy: null, unitId: 2);
            _nonArcanistDefender.StatSheet.MagicAttack.SetBaseValue(BaseMagicAttack);

            _controller = new ArcanistSynergyController(_activation, null);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
            Object.DestroyImmediate(_arcanistGo);
            Object.DestroyImmediate(_nonArcanistGo);

            foreach (ScriptableObject asset in _createdAssets)
            {
                Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        // ── 시너지 보유 Defender가 전장 진입 시 ArcanistSSE가 적용된다 ──

        [Test]
        public void PlacementChanged_BattleArea_AppliesSSEToSynergyDefender()
        {
            PublishPlacement(_arcanistDefender, Placement.BattleArea);

            float expected = BaseMagicAttack + Tier1ArcanistSpellPower;
            Assert.AreEqual(expected, _arcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        // ── 비보유 Defender가 전장 진입 시 SpellPowerEffect가 적용된다 ──

        [Test]
        public void PlacementChanged_BattleArea_AppliesSpellPowerToNonSynergyDefender()
        {
            // 시너지 활성화를 위해 arcanist가 먼저 진입
            PublishPlacement(_arcanistDefender, Placement.BattleArea);
            PublishPlacement(_nonArcanistDefender, Placement.BattleArea);

            float expected = BaseMagicAttack + Tier1SpellPower;
            Assert.AreEqual(expected, _nonArcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        // ── 비보유 Defender가 퇴장 시 SpellPowerEffect가 해제된다 ──

        [Test]
        public void PlacementChanged_WaitingArea_RemovesSpellPowerFromNonSynergyDefender()
        {
            PublishPlacement(_arcanistDefender, Placement.BattleArea);
            PublishPlacement(_nonArcanistDefender, Placement.BattleArea);

            PublishPlacement(_nonArcanistDefender, Placement.WaitingArea);

            Assert.AreEqual(BaseMagicAttack, _nonArcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
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

            // UnitDefinitionData 설정
            var unitDef = ScriptableObject.CreateInstance<UnitDefinitionData>();
            _createdAssets.Add(unitDef);
            SetPrivateField(unitDef, "id", unitId);
            if (synergy != null)
            {
                SetPrivateField(unitDef, "summonerEffect", synergy);
            }

            // UnitLoadOutData 설정
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

        /// <summary>테스트용 Defender stub. OnEnable 등 Unity 콜백을 비활성화한다.</summary>
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
