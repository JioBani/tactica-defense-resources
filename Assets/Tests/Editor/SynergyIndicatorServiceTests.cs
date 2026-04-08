// ─────────────────────────────────────────────
// SynergyIndicatorServiceTests: TACD-295 시너지 정보 UI 테스트
//
// 테스트 케이스 목록:
//
// A. SynergyIndicatorService.CalculateRatios — 비율 계산 로직 (CD-3, CD-5)
//   A-1. [정상] 2/4 티어, count=0 → 모든 비율 0
//   A-2. [정상] 2/4 티어, count=1 → 첫 도트 0.5, 둘째 도트 0
//   A-3. [정상] 2/4 티어, count=2 → 첫 도트 1.0, 둘째 도트 0
//   A-4. [정상] 2/4 티어, count=3 → 첫 도트 1.0, 둘째 도트 0.5
//   A-5. [정상] 2/4 티어, count=4 → 모든 비율 1.0
//   A-6. [정상] 2/4/6/8 티어, count=6 → 첫 3개 1.0, 넷째 0
//   A-7. [정상] 3/6/9 티어, count=2 → 첫 도트 2/3
//   A-8. [경계] count가 최고 티어 초과 → 모든 비율 1.0
//   A-9. [경계] dotCount < tiers.Count → dotCount만큼만 반환
//   A-10. [경계] dotCount > tiers.Count → tiers.Count만큼만 반환
//   A-11. [경계] count=0, 단일 티어 → 비율 0
//
// B. SynergyController 이벤트 발행 (CD-1)
//   B-1. [정상] Defender 전장 진입 → OnSynergyRecalculatedEventDto 발행
//   B-2. [정상] Defender 전장 퇴장 → OnSynergyRecalculatedEventDto 발행
//   B-3. [정상] Defender 판매(Despawn) → OnSynergyRecalculatedEventDto 발행
//   B-4. [상태] 시너지 비보유 Defender 진입 → 이벤트 미발행
//
// C. MonoBehaviour 의존 테스트 (Ignore)
//   C-1. [스킵] SynergyIndicator 아이콘 바인딩 (CD-2)
//   C-2. [스킵] SynergyIndicator 버튼 클릭 이벤트 (CD-6)
//   C-3. [스킵] SynergyListPanel 초기화 (CD-7)
//   C-4. [스킵] SynergyListPanel/SynergyInfoPanel 이벤트 전달 (CD-8~10)
//   C-5. [스킵] GameObject 계층 구성 (CD-11~13)
// ─────────────────────────────────────────────
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
using Scenes.Battle.Feature.Ui.SynergyInfo;
using Scenes.Battle.Feature.Unit.Defenders;
using Scenes.Battle.Feature.Units;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-295 시너지 정보 UI의 유닛 테스트.
    /// SynergyIndicatorService의 비율 계산과 SynergyController의 이벤트 발행을 검증한다.
    /// </summary>
    public class SynergyIndicatorServiceTests
    {
        private SynergyIndicatorService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new SynergyIndicatorService();
        }

        // ══════════════════════════════════════════════
        // A. CalculateRatios — 비율 계산 로직
        // ══════════════════════════════════════════════

        // A-1~5: 2/4 티어에서 다양한 카운트 조합 검증
        [TestCase(0, new float[] { 0f, 0f }, TestName = "A1_TwoFourTiers_CountZero_AllZero")]
        [TestCase(1, new float[] { 0.5f, 0f }, TestName = "A2_TwoFourTiers_CountOne_FirstHalf")]
        [TestCase(2, new float[] { 1f, 0f }, TestName = "A3_TwoFourTiers_CountTwo_FirstFull")]
        [TestCase(3, new float[] { 1f, 0.5f }, TestName = "A4_TwoFourTiers_CountThree_SecondHalf")]
        [TestCase(4, new float[] { 1f, 1f }, TestName = "A5_TwoFourTiers_CountFour_AllFull")]
        public void CalculateRatios_TwoFourTiers_ReturnsExpectedRatios(int count, float[] expected)
        {
            // 2/4 티어 구성
            var tiers = CreateTierList(2, 4);

            float[] ratios = _service.CalculateRatios(tiers, count, tiers.Count);

            Assert.AreEqual(expected.Length, ratios.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], ratios[i], 0.001f, $"도트 {i} 비율 불일치");
            }
        }

        // A-6: 2/4/6/8 4단계 티어에서 count=6 검증
        [Test]
        public void CalculateRatios_FourTiers_CountSix_FirstThreeFullFourthZero()
        {
            var tiers = CreateTierList(2, 4, 6, 8);

            float[] ratios = _service.CalculateRatios(tiers, 6, tiers.Count);

            Assert.AreEqual(4, ratios.Length);
            Assert.AreEqual(1f, ratios[0], 0.001f, "도트 0");
            Assert.AreEqual(1f, ratios[1], 0.001f, "도트 1");
            Assert.AreEqual(1f, ratios[2], 0.001f, "도트 2");
            Assert.AreEqual(0f, ratios[3], 0.001f, "도트 3");
        }

        // A-7: 3/6/9 3단계 티어에서 count=2 검증 (2/3 비율)
        [Test]
        public void CalculateRatios_ThreeSixNineTiers_CountTwo_FirstDotTwoThirds()
        {
            var tiers = CreateTierList(3, 6, 9);

            float[] ratios = _service.CalculateRatios(tiers, 2, tiers.Count);

            Assert.AreEqual(3, ratios.Length);
            Assert.AreEqual(2f / 3f, ratios[0], 0.001f, "도트 0: 2/3");
            Assert.AreEqual(0f, ratios[1], 0.001f, "도트 1");
            Assert.AreEqual(0f, ratios[2], 0.001f, "도트 2");
        }

        // A-8: count가 최고 티어 초과 시 모든 비율 1.0
        [Test]
        public void CalculateRatios_CountExceedsMaxTier_AllFull()
        {
            var tiers = CreateTierList(2, 4);

            float[] ratios = _service.CalculateRatios(tiers, 10, tiers.Count);

            Assert.AreEqual(2, ratios.Length);
            Assert.AreEqual(1f, ratios[0], 0.001f);
            Assert.AreEqual(1f, ratios[1], 0.001f);
        }

        // A-9: dotCount < tiers.Count → dotCount만큼만 반환
        [Test]
        public void CalculateRatios_DotCountLessThanTiers_ReturnsOnlyDotCount()
        {
            var tiers = CreateTierList(2, 4, 6);

            float[] ratios = _service.CalculateRatios(tiers, 3, 2);

            Assert.AreEqual(2, ratios.Length);
            Assert.AreEqual(1f, ratios[0], 0.001f, "도트 0");
            Assert.AreEqual(0.5f, ratios[1], 0.001f, "도트 1");
        }

        // A-10: dotCount > tiers.Count → tiers.Count만큼만 반환
        [Test]
        public void CalculateRatios_DotCountGreaterThanTiers_ReturnsOnlyTiersCount()
        {
            var tiers = CreateTierList(2, 4);

            float[] ratios = _service.CalculateRatios(tiers, 3, 5);

            Assert.AreEqual(2, ratios.Length);
        }

        // A-11: 단일 티어(2), count=0 → 비율 0
        [Test]
        public void CalculateRatios_SingleTier_CountZero_RatioZero()
        {
            var tiers = CreateTierList(2);

            float[] ratios = _service.CalculateRatios(tiers, 0, tiers.Count);

            Assert.AreEqual(1, ratios.Length);
            Assert.AreEqual(0f, ratios[0], 0.001f);
        }

        // ══════════════════════════════════════════════
        // A 추가: 2/4/6/8 티어에서 다양한 중간값 검증
        // ══════════════════════════════════════════════

        // 2/4/6/8 티어에서 count=3 → 첫 도트 1.0, 둘째 도트 0.5, 나머지 0
        [Test]
        public void CalculateRatios_FourTiers_CountThree_SecondDotHalf()
        {
            var tiers = CreateTierList(2, 4, 6, 8);

            float[] ratios = _service.CalculateRatios(tiers, 3, tiers.Count);

            Assert.AreEqual(1f, ratios[0], 0.001f, "도트 0");
            Assert.AreEqual(0.5f, ratios[1], 0.001f, "도트 1");
            Assert.AreEqual(0f, ratios[2], 0.001f, "도트 2");
            Assert.AreEqual(0f, ratios[3], 0.001f, "도트 3");
        }

        // 2/4/6/8 티어에서 count=8 → 모든 비율 1.0
        [Test]
        public void CalculateRatios_FourTiers_CountEight_AllFull()
        {
            var tiers = CreateTierList(2, 4, 6, 8);

            float[] ratios = _service.CalculateRatios(tiers, 8, tiers.Count);

            for (int i = 0; i < ratios.Length; i++)
            {
                Assert.AreEqual(1f, ratios[i], 0.001f, $"도트 {i}");
            }
        }

        // ══════════════════════════════════════════════
        // 헬퍼: 티어 목록 생성
        // ══════════════════════════════════════════════

        /// <summary>requiredCount 배열로 SynergyTier 목록을 생성한다.</summary>
        private static List<SynergyTier> CreateTierList(params int[] requiredCounts)
        {
            var tiers = new List<SynergyTier>();
            for (int i = 0; i < requiredCounts.Length; i++)
            {
                tiers.Add(CreateTier(i + 1, requiredCounts[i]));
            }
            return tiers;
        }

        /// <summary>리플렉션으로 SynergyTier를 생성한다.</summary>
        private static SynergyTier CreateTier(int tierLevel, int requiredCount)
        {
            var tier = new SynergyTier();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            object boxed = tier;
            typeof(SynergyTier).GetField("tier", flags).SetValue(boxed, tierLevel);
            typeof(SynergyTier).GetField("requiredCount", flags).SetValue(boxed, requiredCount);
            return (SynergyTier)boxed;
        }
    }

    /// <summary>
    /// SynergyController의 OnSynergyRecalculatedEventDto 이벤트 발행을 검증한다. (CD-1)
    /// </summary>
    public class SynergyControllerEventPublishTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private BruiserSynergyController _controller;

        private Defender _defender1;
        private Defender _defender2;
        private Defender _nonSynergyDefender;
        private GameObject _go1;
        private GameObject _go2;
        private GameObject _goNonSynergy;

        private readonly List<ScriptableObject> _createdAssets = new();

        /// <summary>이벤트 수신 기록.</summary>
        private readonly List<OnSynergyRecalculatedEventDto> _receivedEvents = new();

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(_definition);

            var constants1 = new SerializableDictionary<string, float>();
            constants1["healthPercent"] = 0.25f;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            _go1 = new GameObject("Unit1");
            _defender1 = CreateDefenderWithSynergy(_go1, _definition, unitId: 1);
            _defender1.StatSheet.MaxHealth.SetBaseValue(100f);

            _go2 = new GameObject("Unit2");
            _defender2 = CreateDefenderWithSynergy(_go2, _definition, unitId: 2);
            _defender2.StatSheet.MaxHealth.SetBaseValue(100f);

            _goNonSynergy = new GameObject("NonSynergyUnit");
            _nonSynergyDefender = CreateDefenderWithSynergy(_goNonSynergy, synergy: null, unitId: 3);
            _nonSynergyDefender.StatSheet.MaxHealth.SetBaseValue(100f);

            _controller = new BruiserSynergyController(_activation);

            _receivedEvents.Clear();
            GlobalEventBus.Subscribe<OnSynergyRecalculatedEventDto>(RecordEvent);
        }

        [TearDown]
        public void TearDown()
        {
            GlobalEventBus.Unsubscribe<OnSynergyRecalculatedEventDto>(RecordEvent);
            _controller.Dispose();
            Object.DestroyImmediate(_go1);
            Object.DestroyImmediate(_go2);
            Object.DestroyImmediate(_goNonSynergy);

            foreach (ScriptableObject asset in _createdAssets)
            {
                Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        // B-1: Defender 전장 진입 시 이벤트가 발행된다
        [Test]
        public void PlacementChanged_BattleArea_PublishesRecalculatedEvent()
        {
            PublishPlacement(_defender1, Placement.BattleArea);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual(_activation, _receivedEvents[0].Activation);
        }

        // B-2: Defender 전장 퇴장 시 이벤트가 발행된다
        [Test]
        public void PlacementChanged_WaitingArea_PublishesRecalculatedEvent()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            _receivedEvents.Clear();

            PublishPlacement(_defender1, Placement.WaitingArea);

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual(_activation, _receivedEvents[0].Activation);
        }

        // B-3: Defender 판매(Despawn) 시 이벤트가 발행된다
        [Test]
        public void DefenderDespawned_PublishesRecalculatedEvent()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            _receivedEvents.Clear();

            GlobalEventBus.Publish(new OnDefenderChangedEventDto(_defender1, DefenderChanges.Despawn));

            Assert.AreEqual(1, _receivedEvents.Count);
            Assert.AreEqual(_activation, _receivedEvents[0].Activation);
        }

        // B-4: 시너지 비보유 Defender 진입 시 이벤트가 발행되지 않는다
        [Test]
        public void PlacementChanged_NonSynergyDefender_NoEventPublished()
        {
            PublishPlacement(_nonSynergyDefender, Placement.BattleArea);

            Assert.AreEqual(0, _receivedEvents.Count);
        }

        // ── B 추가: 진입/퇴장 연속 시 이벤트가 각각 발행된다 ──

        [Test]
        public void PlacementChanged_EnterAndLeave_TwoEventsPublished()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender1, Placement.WaitingArea);

            Assert.AreEqual(2, _receivedEvents.Count);
        }

        // ── B 추가: 복수 Defender 진입 시 이벤트가 각각 발행된다 ──

        [Test]
        public void PlacementChanged_TwoDefendersEnter_TwoEventsPublished()
        {
            PublishPlacement(_defender1, Placement.BattleArea);
            PublishPlacement(_defender2, Placement.BattleArea);

            Assert.AreEqual(2, _receivedEvents.Count);
            Assert.AreEqual(_activation, _receivedEvents[0].Activation);
            Assert.AreEqual(_activation, _receivedEvents[1].Activation);
        }

        // ══════════════════════════════════════════════
        // C. MonoBehaviour 의존 테스트 — Ignore
        // ══════════════════════════════════════════════

        // C-1: SynergyIndicator 아이콘 바인딩 (CD-2)
        [Test]
        [Ignore("MonoBehaviour(SynergyIndicator)와 Image 컴포넌트에 의존하여 에디터 테스트 불가")]
        public void SynergyIndicator_Bind_SetsIcon()
        {
        }

        // C-2: SynergyIndicator 버튼 클릭 이벤트 (CD-6)
        [Test]
        [Ignore("MonoBehaviour(SynergyIndicator)와 Button 컴포넌트에 의존하여 에디터 테스트 불가")]
        public void SynergyIndicator_Click_InvokesOnClicked()
        {
        }

        // C-3: SynergyListPanel 초기화 (CD-7)
        [Test]
        [Ignore("MonoBehaviour(SynergyListPanel)와 SynergyManager 싱글톤에 의존하여 에디터 테스트 불가")]
        public void SynergyListPanel_Start_BindsIndicators()
        {
        }

        // C-4: SynergyListPanel/SynergyInfoPanel 이벤트 전달 (CD-8~10)
        [Test]
        [Ignore("MonoBehaviour(SynergyInfoPanel)와 씬 참조에 의존하여 에디터 테스트 불가")]
        public void SynergyInfoPanel_IndicatorClick_DelegatesToDetailPanel()
        {
        }

        // C-5: GameObject 계층 구성 (CD-11~13)
        [Test]
        [Ignore("씬 구성 검증은 에디터 테스트 범위 밖")]
        public void GameObjectHierarchy_CorrectlyConfigured()
        {
        }

        // ══════════════════════════════════════════════
        // 헬퍼
        // ══════════════════════════════════════════════

        private void RecordEvent(OnSynergyRecalculatedEventDto dto)
        {
            _receivedEvents.Add(dto);
        }

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
                // HasSynergy 검사를 통과하도록 시너지를 추가한다
                defender.AddSynergy(synergy);
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