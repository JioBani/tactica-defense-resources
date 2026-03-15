using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.SerializableDictionary;
using Common.Scripts.StatusEffect;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyControllers;
using Scenes.Battle.Feature.Synergy.SynergyEffects;
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
        private DefenderManager _defenderManager;
        private GameObject _defenderManagerGo;

        private Defender _arcanistDefender;
        private Defender _nonArcanistDefender;
        private GameObject _arcanistGo;
        private GameObject _nonArcanistGo;

        private const float Tier1SpellPower = 15f;
        private const float Tier1ArcanistSpellPower = 25f;
        private const float BaseMagicAttack = 10f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            var constants1 = new SerializableDictionary<string, float>();
            constants1["spellPower"] = Tier1SpellPower;
            constants1["arcanistSpellPower"] = Tier1ArcanistSpellPower;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);
            _activation.Recalculate(2); // 1티어 활성화

            _defenderManagerGo = new GameObject("DefenderManager");
            _defenderManager = _defenderManagerGo.AddComponent<DefenderManager>();

            _arcanistGo = new GameObject("ArcanistUnit");
            _arcanistDefender = CreateDefenderWithSEC(_arcanistGo);
            _arcanistDefender.StatSheet.MagicAttack.SetBaseValue(BaseMagicAttack);

            _nonArcanistGo = new GameObject("NonArcanistUnit");
            _nonArcanistDefender = CreateDefenderWithSEC(_nonArcanistGo);
            _nonArcanistDefender.StatSheet.MagicAttack.SetBaseValue(BaseMagicAttack);

            // DefenderManager에 비전 마법사가 아닌 유닛을 BattleArea로 등록
            SetBattleAreaDefenders(_defenderManager, new List<Defender> { _arcanistDefender, _nonArcanistDefender });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_arcanistGo);
            Object.DestroyImmediate(_nonArcanistGo);
            Object.DestroyImmediate(_defenderManagerGo);
            Object.DestroyImmediate(_definition);
        }

        // ── 시너지 보유 Defender에 ArcanistSSE가 적용된다 ──

        [Test]
        public void OnActivated_AppliesSSEToSynergyDefenders()
        {
            var controller = new ArcanistSynergyController(_activation, _defenderManager, null);
            var synergyDefenders = new List<Defender> { _arcanistDefender };

            controller.OnActivated(synergyDefenders);

            float expected = BaseMagicAttack + Tier1ArcanistSpellPower;
            Assert.AreEqual(expected, _arcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        // ── 비보유 Defender에 ArcanistSpellPowerEffect가 적용된다 ──

        [Test]
        public void OnActivated_AppliesSpellPowerEffectToNonSynergyDefenders()
        {
            var controller = new ArcanistSynergyController(_activation, _defenderManager, null);
            var synergyDefenders = new List<Defender> { _arcanistDefender };

            controller.OnActivated(synergyDefenders);

            float expected = BaseMagicAttack + Tier1SpellPower;
            Assert.AreEqual(expected, _nonArcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        // ── 이미 적용된 Defender에 중복 Apply 되지 않는다 ──

        [Test]
        public void OnActivated_Twice_DoesNotDuplicateEffects()
        {
            var controller = new ArcanistSynergyController(_activation, _defenderManager, null);
            var synergyDefenders = new List<Defender> { _arcanistDefender };

            controller.OnActivated(synergyDefenders);
            controller.OnActivated(synergyDefenders);

            float expectedArcanist = BaseMagicAttack + Tier1ArcanistSpellPower;
            float expectedNonArcanist = BaseMagicAttack + Tier1SpellPower;
            Assert.AreEqual(expectedArcanist, _arcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
            Assert.AreEqual(expectedNonArcanist, _nonArcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        // ── OnDeactivated 후 다시 OnActivated 시 재적용된다 ──

        [Test]
        public void AfterDeactivated_OnActivated_ReappliesEffects()
        {
            var controller = new ArcanistSynergyController(_activation, _defenderManager, null);
            var synergyDefenders = new List<Defender> { _arcanistDefender };

            controller.OnActivated(synergyDefenders);
            // 비활성화 시뮬레이션: 수정자 제거는 SSE/SE가 ActiveTier 구독으로 처리하므로
            // 여기서는 Controller의 추적 초기화만 테스트
            controller.OnDeactivated();
            controller.OnActivated(synergyDefenders);

            // 두 번 적용되어야 함 (첫 번째 것은 아직 제거 안 됨 — 이건 SSE의 책임)
            // Controller 관점에서는 추적이 초기화되어 다시 Apply가 호출되었는지만 확인
            float expectedArcanist = BaseMagicAttack + Tier1ArcanistSpellPower + Tier1ArcanistSpellPower;
            Assert.AreEqual(expectedArcanist, _arcanistDefender.StatSheet.MagicAttack.CurrentValue, 0.01f);
        }

        /// <summary>Defender에 StatusEffectController를 추가하고 리플렉션으로 연결한다.</summary>
        private static Defender CreateDefenderWithSEC(GameObject go)
        {
            var sec = go.AddComponent<StatusEffectController>();
            var defender = go.AddComponent<StubDefender>();

            var secField = typeof(Unit)
                .GetField("statusEffectController", BindingFlags.NonPublic | BindingFlags.Instance);
            secField.SetValue(defender, sec);

            return defender;
        }

        /// <summary>리플렉션으로 DefenderManager의 내부 리스트에 Defender를 추가한다.</summary>
        private static void SetBattleAreaDefenders(DefenderManager manager, List<Defender> defenders)
        {
            // DefenderManager.GetBattleAreaDefenders()는 units 리스트에서 Placement == BattleArea인 것을 필터링한다.
            // units 필드에 직접 추가하고 Placement를 BattleArea로 설정한다.
            var unitsField = typeof(DefenderManager)
                .GetField("units", BindingFlags.NonPublic | BindingFlags.Instance);

            if (unitsField == null)
            {
                // 필드명이 다를 수 있으므로 모든 필드를 탐색
                foreach (var field in typeof(DefenderManager).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.FieldType == typeof(List<Defender>))
                    {
                        unitsField = field;
                        break;
                    }
                }
            }

            if (unitsField != null)
            {
                var list = (List<Defender>)unitsField.GetValue(manager);
                if (list == null)
                {
                    list = new List<Defender>();
                    unitsField.SetValue(manager, list);
                }

                foreach (var defender in defenders)
                {
                    list.Add(defender);
                    // Placement를 BattleArea로 설정
                    var placementProp = typeof(Defender)
                        .GetProperty("Placement", BindingFlags.Public | BindingFlags.Instance);
                    placementProp?.SetValue(defender, Placement.BattleArea);
                }
            }
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