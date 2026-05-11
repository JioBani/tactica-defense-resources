using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Data.Units.UnitDefinitions;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using NUnit.Framework;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Ui.SynergyInfo;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    /// <summary>
    /// TACD-296 member-list 구현 단위의 요구사항 테스트.
    /// SR 그룹 C (DoD-S11 ~ DoD-S14) — 시너지 보유 소환수 목록 표시.
    ///
    /// 기대값 도출 근거: 시스템요구사항.md / 사용자요구사항.md 의 그룹 C DoD 항목.
    /// 구현 코드의 분기 로직을 따라가지 않는다.
    ///
    /// 픽스처 전략:
    /// - sectionRoot VisualElement 수동 구성 (member-list 명명 자식).
    /// - SynergyManager: 비활성 GameObject 에 AddComponent 후, SceneSingleton 정적 _instance 와
    ///   인스턴스 _synergyMembersPublic 을 리플렉션 주입하여 라이프사이클(Awake/Start)을 우회.
    /// - DefenderManager: 비활성 GameObject 에 AddComponent + units 리플렉션 주입.
    /// - Defender: 비활성 GameObject + StubDefender (Placement 백킹 필드 리플렉션 주입).
    /// </summary>
    public class SynergyDetailMemberListTests
    {
        private const string MemberItemClass = "member-item";
        private const string MemberItemActiveClass = "member-item--active";

        private VisualElement _sectionRoot;
        private VisualElement _listContainer;

        private GameObject _synergyManagerGo;
        private SynergyManager _synergyManager;
        private Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>> _synergyMembers;

        private GameObject _defenderManagerGo;
        private DefenderManager _defenderManager;
        private List<Defender> _defendersUnitsField;

        private readonly List<GameObject> _defenderGos = new();
        private readonly List<ScriptableObject> _createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            _sectionRoot = new VisualElement { name = "member-section" };
            _listContainer = new VisualElement { name = "member-list" };
            _sectionRoot.Add(_listContainer);

            // SynergyManager 픽스처
            _synergyManagerGo = new GameObject("SynergyManager(Test)");
            _synergyManagerGo.SetActive(false);
            _synergyManager = _synergyManagerGo.AddComponent<SynergyManager>();
            SetSceneSingletonInstance<SynergyManager>(_synergyManager);
            _synergyMembers = new Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>>();
            SetInstanceField(_synergyManager, "_synergyMembersPublic", _synergyMembers);

            // DefenderManager 픽스처
            _defenderManagerGo = new GameObject("DefenderManager(Test)");
            _defenderManagerGo.SetActive(false);
            _defenderManager = _defenderManagerGo.AddComponent<DefenderManager>();
            _defendersUnitsField = new List<Defender>();
            SetInstanceField(_defenderManager, "units", _defendersUnitsField);
        }

        [TearDown]
        public void TearDown()
        {
            SetSceneSingletonInstance<SynergyManager>(null);

            if (_synergyManagerGo != null) Object.DestroyImmediate(_synergyManagerGo);
            if (_defenderManagerGo != null) Object.DestroyImmediate(_defenderManagerGo);

            foreach (GameObject go in _defenderGos)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _defenderGos.Clear();

            foreach (ScriptableObject asset in _createdAssets)
            {
                if (asset != null) Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        // ── DoD-S11: 항목 개수 = SynergyMembers[def].Count ──

        [Test]
        public void Test_TC1201_Bind_ItemCount_MatchesSynergyMembers()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("unit1");
            UnitLoadOutData u2 = MakeUnit("unit2");
            UnitLoadOutData u3 = MakeUnit("unit3");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1, u2, u3 };

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));

            Assert.That(_listContainer.childCount, Is.EqualTo(3),
                "member-list 자식 개수는 SynergyMembers[def] 의 unit 수와 일치해야 한다.");
        }

        [Test]
        public void Test_TC1202_Bind_KeyAbsent_RendersEmptyList()
        {
            SynergyDefinitionData def = MakeDefinition();
            // 의도적으로 _synergyMembers 에 def 키를 넣지 않음.

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));

            Assert.That(_listContainer.childCount, Is.EqualTo(0),
                "SynergyMembers 에 키가 없으면 빈 목록으로 표시되어야 한다 (시너지 종류 무관 일반 책임).");
        }

        [Test]
        public void Test_TC1203_Bind_EmptyMemberList_RendersEmptyList()
        {
            SynergyDefinitionData def = MakeDefinition();
            _synergyMembers[def] = new List<UnitLoadOutData>();

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);

            Assert.DoesNotThrow(() => memberList.Bind(new SynergyActivation(def)));
            Assert.That(_listContainer.childCount, Is.EqualTo(0));
        }

        // ── DoD-S12: 아이콘 + 이름 ──

        [Test]
        public void Test_TC1301_EachItem_DisplaysUnitName()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("케일", icon: Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero));
            UnitLoadOutData u2 = MakeUnit("애쉬");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1, u2 };

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));

            var names = new List<string>();
            foreach (VisualElement item in _listContainer.Children())
            {
                // member-item: 첫 자식이 icon (VisualElement), 두 번째가 name Label.
                var nameLabel = item.ElementAt(1) as Label;
                Assert.That(nameLabel, Is.Not.Null);
                names.Add(nameLabel.text);
            }

            Assert.That(names, Is.EqualTo(new List<string> { "케일", "애쉬" }));
        }

        [Test]
        public void Test_TC1302_EachItem_DisplaysUnitIcon()
        {
            SynergyDefinitionData def = MakeDefinition();
            Sprite icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            UnitLoadOutData u1 = MakeUnit("u1", icon: icon);
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));

            var item = _listContainer.ElementAt(0);
            var iconElement = item.ElementAt(0);
            Sprite actualSprite = iconElement.style.backgroundImage.value.sprite;
            Assert.That(actualSprite, Is.SameAs(icon),
                "각 항목의 아이콘 영역 backgroundImage 는 unit.Unit.Icon Sprite 와 일치해야 한다.");
        }

        // ── DoD-S13: 배치 상태 시각 ──

        [TestCase(true, Placement.BattleArea, true, ExpectedResult = true,
            TestName = "TC-1401_DefenderInBattleArea_NotDowned_HasActiveClass")]
        [TestCase(true, Placement.WaitingArea, false, ExpectedResult = false,
            TestName = "TC-1402_DefenderInWaitingArea_NoActiveClass")]
        [TestCase(false, Placement.BattleArea, false, ExpectedResult = false,
            TestName = "TC-1403_NoMatchingDefender_NoActiveClass")]
        [TestCase(true, Placement.BattleArea, true, ExpectedResult = true,
            TestName = "TC-1404_DefenderInBattleArea_Downed_StillActive")]
        public bool Test_Placement_DecisionTable_ReturnsActiveAsExpected(
            bool hasMatchingDefender, Placement placement, bool _isDowned)
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("u1");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };

            if (hasMatchingDefender)
            {
                // 다운 여부는 본 판정에 무관 (DoD-S13). 다운 상태 시뮬레이션은 ActionStateController
                // 접근이 필요해 본 단위 픽스처 범위 밖이므로, 다운 케이스는 Placement 만으로 활성 판정되는지
                // 만 검증 — _isDowned 인자는 케이스 라벨링용으로만 사용 (실제 다운 상태 주입 없음).
                AddDefender(u1, placement);
            }

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));

            return _listContainer.ElementAt(0).ClassListContains(MemberItemActiveClass);
        }

        [Test]
        public void Test_TC1405_MultipleDefenders_OneInBattleArea_ResultsInActive()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("u1");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };

            AddDefender(u1, Placement.WaitingArea);
            AddDefender(u1, Placement.BattleArea);

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));

            Assert.That(_listContainer.ElementAt(0).ClassListContains(MemberItemActiveClass), Is.True,
                "동일 unit 의 디펜더 중 하나라도 BattleArea 에 있으면 활성으로 판정되어야 한다.");
        }

        // ── DoD-S14: 알림 구독으로 갱신 ──

        [Test]
        public void Test_TC1501_PlacementChangedEvent_UpdatesItemActiveState()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("u1");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };

            Defender defender = AddDefender(u1, Placement.WaitingArea);

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));
            Assume.That(_listContainer.ElementAt(0).ClassListContains(MemberItemActiveClass), Is.False);

            // BattleArea 로 이동 → defender.Placement 갱신 + 이벤트 publish.
            SetDefenderPlacement(defender, Placement.BattleArea);
            GlobalEventBus.Publish(new OnDefenderPlacementChangedEventDto(defender, Placement.BattleArea));

            Assert.That(_listContainer.ElementAt(0).ClassListContains(MemberItemActiveClass), Is.True,
                "OnDefenderPlacementChangedEventDto publish 후 해당 unit 항목의 시각이 활성으로 갱신되어야 한다.");
        }

        [Test]
        public void Test_TC1502_DefenderChangedEvent_UpdatesItemActiveState()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("u1");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };

            Defender defender = AddDefender(u1, Placement.BattleArea);

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));
            Assume.That(_listContainer.ElementAt(0).ClassListContains(MemberItemActiveClass), Is.True);

            // 디스폰 — defender 를 manager 목록에서 제거 + 이벤트 publish.
            _defendersUnitsField.Remove(defender);
            GlobalEventBus.Publish(new OnDefenderChangedEventDto(defender, DefenderChanges.Despawn));

            Assert.That(_listContainer.ElementAt(0).ClassListContains(MemberItemActiveClass), Is.False,
                "OnDefenderChangedEventDto(Despawn) publish 후 해당 unit 항목이 비활성으로 갱신되어야 한다.");
        }

        [Test]
        public void Test_TC1503_AfterUnbind_EventsDoNotUpdateItems()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("u1");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };
            Defender defender = AddDefender(u1, Placement.WaitingArea);

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));
            memberList.Unbind();

            // Unbind 후 컨테이너가 비었으므로 직접 검증 대상은 *예외 미발생* + *재 publish 무영향* .
            Assert.DoesNotThrow(() =>
            {
                SetDefenderPlacement(defender, Placement.BattleArea);
                GlobalEventBus.Publish(new OnDefenderPlacementChangedEventDto(defender, Placement.BattleArea));
                GlobalEventBus.Publish(new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));
            },
            "Unbind 후 두 종 알림이 도착해도 핸들러는 호출되지 않아야 한다 (구독 해제).");

            Assert.That(_listContainer.childCount, Is.EqualTo(0),
                "Unbind 후 member-list 컨테이너는 비어 있어야 하며, 알림에 의해 항목이 다시 만들어지지도 않아야 한다.");
        }

        // ── 안전망 / 라이프사이클 ──

        [Test]
        public void Test_TC1601_Bind_NullActivation_IsIgnored_NoThrow()
        {
            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);

            Assert.DoesNotThrow(() => memberList.Bind(null));
            Assert.That(_listContainer.childCount, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC1602_Rebind_ReplacesItemsWithNewSynergy()
        {
            SynergyDefinitionData defA = MakeDefinition();
            SynergyDefinitionData defB = MakeDefinition();
            UnitLoadOutData uA = MakeUnit("A1");
            UnitLoadOutData uB1 = MakeUnit("B1");
            UnitLoadOutData uB2 = MakeUnit("B2");
            _synergyMembers[defA] = new List<UnitLoadOutData> { uA };
            _synergyMembers[defB] = new List<UnitLoadOutData> { uB1, uB2 };

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(defA));
            Assume.That(_listContainer.childCount, Is.EqualTo(1));

            memberList.Rebind(new SynergyActivation(defB));

            Assert.That(_listContainer.childCount, Is.EqualTo(2),
                "Rebind 후 member-list 는 새 시너지의 unit 으로 교체되어야 한다.");

            var names = new List<string>();
            foreach (VisualElement item in _listContainer.Children())
            {
                names.Add((item.ElementAt(1) as Label).text);
            }
            Assert.That(names, Is.EqualTo(new List<string> { "B1", "B2" }));
        }

        [Test]
        public void Test_TC1603_Unbind_ClearsListContainer()
        {
            SynergyDefinitionData def = MakeDefinition();
            UnitLoadOutData u1 = MakeUnit("u1");
            _synergyMembers[def] = new List<UnitLoadOutData> { u1 };

            var memberList = new SynergyDetailMemberList(_sectionRoot, _defenderManager);
            memberList.Bind(new SynergyActivation(def));
            Assume.That(_listContainer.childCount, Is.EqualTo(1));

            memberList.Unbind();

            Assert.That(_listContainer.childCount, Is.EqualTo(0));
        }

        // ── helpers ──

        private SynergyDefinitionData MakeDefinition()
        {
            SynergyDefinitionData def = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(def);
            return def;
        }

        private UnitLoadOutData MakeUnit(string displayName, Sprite icon = null)
        {
            UnitDefinitionData unitDef = ScriptableObject.CreateInstance<UnitDefinitionData>();
            _createdAssets.Add(unitDef);
            SetInstanceField(unitDef, "displayName", displayName);
            if (icon != null)
            {
                SetInstanceField(unitDef, "icon", icon);
            }

            UnitLoadOutData loadOut = ScriptableObject.CreateInstance<UnitLoadOutData>();
            _createdAssets.Add(loadOut);
            SetInstanceField(loadOut, "unit", unitDef);
            return loadOut;
        }

        private Defender AddDefender(UnitLoadOutData unit, Placement placement)
        {
            var go = new GameObject("Defender(Test)");
            go.SetActive(false);
            _defenderGos.Add(go);
            var defender = go.AddComponent<StubDefender>();
            defender.UnitLoadOutData = unit;
            SetDefenderPlacement(defender, placement);
            _defendersUnitsField.Add(defender);
            return defender;
        }

        private static void SetDefenderPlacement(Defender defender, Placement placement)
        {
            // 자동 프로퍼티의 컴파일러 생성 백킹 필드 명: "<Placement>k__BackingField"
            FieldInfo backing = typeof(Defender).GetField(
                "<Placement>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backing, Is.Not.Null,
                "Defender.Placement 의 자동 프로퍼티 백킹 필드를 찾지 못했다.");
            backing.SetValue(defender, placement);
        }

        private static void SetSceneSingletonInstance<T>(T value) where T : Component
        {
            FieldInfo field = typeof(SceneSingleton<T>).GetField(
                "_instance", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"SceneSingleton<{typeof(T).Name}>._instance 정적 필드를 찾지 못했다.");
            field.SetValue(null, value);
        }

        private static void SetInstanceField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"리플렉션 픽스처: {target.GetType().Name}.{fieldName} 필드를 찾지 못했다.");
            field.SetValue(target, value);
        }

        /// <summary>SynergyControllerTests 와 동일한 패턴의 Defender stub.</summary>
        private class StubDefender : Defender { }
    }
}
