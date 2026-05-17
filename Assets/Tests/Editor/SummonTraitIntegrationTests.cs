using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Common.Data.StatusEffects;
using Common.Data.Summoners.SummonerDefinitions;
using Common.Data.Summoners.SummonerFormations;
using Common.Data.Summoners.SummonerLoadOuts;
using Common.Data.Synergies;
using Common.Data.Units.UnitDefinitions;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using Common.Scripts.SerializableDictionary;
using NUnit.Framework;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.SummonTrait;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyControllers;
using Scenes.Battle.Feature.Unit.Defenders;
using Scenes.Battle.Feature.Unit.Summoners;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    /// <summary>
    /// SummonTrait 통합 (post-refactor) 테스트. SynergyManager 단일 클래스로 응집된
    /// 분배·통합·보관·정리·조회 책임을 black-box 검증한다.
    /// </summary>
    public class SummonTraitIntegrationTests
    {
        private readonly List<ScriptableObject> _createdAssets = new();
        private readonly List<GameObject> _createdGameObjects = new();

        [SetUp]
        public void SetUp()
        {
            ResetAllSingletons();
            ResetGlobalEventBus();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _createdGameObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _createdGameObjects.Clear();

            foreach (var asset in _createdAssets)
            {
                if (asset != null) Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();

            ResetAllSingletons();
            ResetGlobalEventBus();
        }

        private static void ResetAllSingletons()
        {
            ResetSceneSingletonInstance<SynergyManager>();
            ResetSceneSingletonInstance<SummonerManager>();
            ResetSceneSingletonInstance<SynergyControllerFactory>();
        }

        // ── reflection helpers ──

        /// <summary>EditMode 에서 자동 발화하지 않는 비공개 메서드를 reflection 으로 호출한다.</summary>
        private static object InvokeNonPublic(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return method?.Invoke(target, args);
        }

        /// <summary>비공개 [SerializeField] 필드에 값을 주입한다.</summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        /// <summary>상속 체인을 따라 비공개 필드를 검색하여 값을 읽는다.</summary>
        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = null;
            var type = target.GetType();
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            return (T)field?.GetValue(target);
        }

        /// <summary>SceneSingleton 의 정적 인스턴스를 설정한다.</summary>
        private static void SetSceneSingletonInstance<T>(T instance) where T : Component
        {
            var instanceField = typeof(SceneSingleton<T>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, instance);
        }

        /// <summary>SceneSingleton 의 정적 인스턴스 캐시를 비운다.</summary>
        private static void ResetSceneSingletonInstance<T>() where T : Component
        {
            var instanceField = typeof(SceneSingleton<T>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }

        /// <summary>GlobalEventBus 의 정적 핸들러 사전을 반환한다.</summary>
        private static IDictionary GetEventBusHandlers()
        {
            var handlersField = typeof(GlobalEventBus)
                .GetField("Handlers", BindingFlags.NonPublic | BindingFlags.Static);
            return handlersField?.GetValue(null) as IDictionary;
        }

        /// <summary>GlobalEventBus 핸들러 사전을 비운다.</summary>
        private static void ResetGlobalEventBus()
        {
            GetEventBusHandlers()?.Clear();
        }

        // ── 픽스처 생성 ──

        /// <summary>UnitDefinitionData (id 지정) + UnitLoadOutData 묶음을 in-memory 로 생성한다.</summary>
        private UnitLoadOutData CreateUnit(int id)
        {
            var unitDef = ScriptableObject.CreateInstance<UnitDefinitionData>();
            _createdAssets.Add(unitDef);
            SetPrivateField(unitDef, "id", id);

            var loadOut = ScriptableObject.CreateInstance<UnitLoadOutData>();
            _createdAssets.Add(loadOut);
            SetPrivateField(loadOut, "unit", unitDef);
            return loadOut;
        }

        /// <summary>SynergyType=SummonTrait 인 SynergyDefinitionData 생성.</summary>
        private SynergyDefinitionData CreateTrait(string displayName = "TestTrait")
        {
            var trait = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(trait);
            SetPrivateField(trait, "synergyType", SynergyType.SummonTrait);
            SetPrivateField(trait, "displayName", displayName);
            return trait;
        }

        /// <summary>SynergyType=SummonerEffect + 지정된 SynergyId 인 SynergyDefinitionData 생성.</summary>
        private SynergyDefinitionData CreateSummonerEffect(SynergyId id = SynergyId.Bruiser, string displayName = "TestSummonerEffect")
        {
            var effect = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(effect);
            SetPrivateField(effect, "synergyType", SynergyType.SummonerEffect);
            SetPrivateField(effect, "id", id);
            SetPrivateField(effect, "displayName", displayName);
            return effect;
        }

        /// <summary>주어진 SummonPool 과 SummonerEffect 를 가진 SummonerLoadOutData 생성.</summary>
        private SummonerLoadOutData CreateSummoner(UnitLoadOutData[] summonPool, SynergyDefinitionData summonerEffect)
        {
            var summonerDef = ScriptableObject.CreateInstance<SummonerDefinitionData>();
            _createdAssets.Add(summonerDef);
            SetPrivateField(summonerDef, "summonPool", summonPool);
            SetPrivateField(summonerDef, "summonerEffect", summonerEffect);

            var summonerLoadOut = ScriptableObject.CreateInstance<SummonerLoadOutData>();
            _createdAssets.Add(summonerLoadOut);
            SetPrivateField(summonerLoadOut, "summoner", summonerDef);
            return summonerLoadOut;
        }

        /// <summary>SummonTraitRegistry SO 생성 (전달된 trait 배열을 traits 필드에 주입).</summary>
        private SummonTraitRegistry CreateRegistry(SynergyDefinitionData[] traits)
        {
            var registry = ScriptableObject.CreateInstance<SummonTraitRegistry>();
            _createdAssets.Add(registry);
            SetPrivateField(registry, "traits", traits ?? new SynergyDefinitionData[0]);
            return registry;
        }

        /// <summary>SummonerManager 인스턴스 (편성 데이터 주입) 생성.</summary>
        private SummonerManager CreateSummonerManagerInScene(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            var go = new GameObject("TestSummonerManager");
            _createdGameObjects.Add(go);
            var manager = go.AddComponent<SummonerManager>();
            manager.SetFormation(new SummonerFormation(summoners));
            SetSceneSingletonInstance(manager);
            return manager;
        }

        /// <summary>SynergyControllerFactory 인스턴스 생성.</summary>
        private SynergyControllerFactory CreateFactoryInScene()
        {
            var go = new GameObject("TestSynergyControllerFactory");
            _createdGameObjects.Add(go);
            var factory = go.AddComponent<SynergyControllerFactory>();
            SetSceneSingletonInstance(factory);
            return factory;
        }

        /// <summary>SynergyManager 인스턴스 생성 (registry 주입 옵션). Start 자동 발화 안 됨.</summary>
        private SynergyManager CreateSynergyManagerInScene(SummonTraitRegistry registry = null)
        {
            var go = new GameObject("TestSynergyManager");
            _createdGameObjects.Add(go);
            var manager = go.AddComponent<SynergyManager>();
            if (registry != null)
            {
                SetPrivateField(manager, "summonTraitRegistry", registry);
            }
            SetSceneSingletonInstance(manager);
            return manager;
        }

        /// <summary>StubDefender + UnitLoadOutData 주입.</summary>
        private Defender CreateStubDefender(UnitLoadOutData unit)
        {
            var go = new GameObject("TestStubDefender");
            _createdGameObjects.Add(go);
            var defender = go.AddComponent<StubDefender>();
            defender.UnitLoadOutData = unit;
            return defender;
        }

        /// <summary>summonTraitMap 필드(SerializableDictionary) 반환.</summary>
        private static SerializableDictionary<UnitLoadOutData, SynergyDefinitionData> GetTraitMap(SynergyManager manager)
        {
            return GetPrivateField<SerializableDictionary<UnitLoadOutData, SynergyDefinitionData>>(manager, "summonTraitMap");
        }

        /// <summary>표준 통합 셋업 — 4 summoners(각 8 unit) + 3 trait + Factory + SynergyManager(registry 주입).</summary>
        private (SynergyManager manager, SummonerLoadOutData[] summoners, SynergyDefinitionData[] traits, SynergyDefinitionData summonerEffect)
            SetupStandardIntegration()
        {
            CreateFactoryInScene();
            var summonerEffect = CreateSummonerEffect(SynergyId.Bruiser, "BruiserEffect");
            var traits = new[] { CreateTrait("TraitA"), CreateTrait("TraitB"), CreateTrait("TraitC") };

            var summoners = new SummonerLoadOutData[4];
            int unitIdCounter = 1;
            for (int i = 0; i < 4; i++)
            {
                var pool = new UnitLoadOutData[8];
                for (int j = 0; j < 8; j++)
                {
                    pool[j] = CreateUnit(unitIdCounter++);
                }
                summoners[i] = CreateSummoner(pool, summonerEffect);
            }
            CreateSummonerManagerInScene(summoners);

            var registry = CreateRegistry(traits);
            var manager = CreateSynergyManagerInScene(registry);
            return (manager, summoners, traits, summonerEffect);
        }

        // ── DoD-S1·S2·S3·S5·S6·S7: Start() 통합 흐름 ──

        [Test]
        public void Test_TC001_Start_Distributes_32_Units_Into_SummonTraitMap()
        {
            var (manager, summoners, traits, _) = SetupStandardIntegration();
            var traitPoolSet = new HashSet<SynergyDefinitionData>(traits);

            InvokeNonPublic(manager, "Start");

            var traitMap = GetTraitMap(manager);
            Assert.That(traitMap.Count, Is.EqualTo(32),
                "4 summoners × 8 units = 32 entries 가 summonTraitMap 에 저장되어야 함");

            foreach (var summoner in summoners)
            {
                foreach (var unit in summoner.Summoner.SummonPool)
                {
                    Assert.That(traitMap.ContainsKey(unit), Is.True, $"unit {unit.name} 배정 누락");
                    Assert.That(traitPoolSet.Contains(traitMap[unit]), Is.True,
                        "배정된 trait 는 풀의 멤버여야 함");
                }
            }
        }

        [Test]
        public void Test_TC002_Start_Coexists_SummonerEffect_And_SummonTrait_In_SynergyActivations()
        {
            var (manager, _, _, summonerEffect) = SetupStandardIntegration();

            InvokeNonPublic(manager, "Start");

            Assert.That(manager.SynergyActivations.ContainsKey(summonerEffect), Is.True,
                "SummonerEffect 활성화가 등록되어야 함");
            // SummonTrait 활성화는 분배 결과의 unique trait 수만큼 등록 — 최소 1 개 이상 존재해야 함
            int summonTraitActivationCount = 0;
            foreach (var kv in manager.SynergyActivations)
            {
                if (kv.Key.SynergyType == SynergyType.SummonTrait)
                {
                    summonTraitActivationCount++;
                }
            }
            Assert.That(summonTraitActivationCount, Is.GreaterThan(0),
                "SummonTrait 활성화도 같은 사전에 등록되어야 함 (시너지 종류 분기 없는 동등 표시)");
        }

        [Test]
        public void Test_TC003_Start_Adds_SummonTrait_Entries_To_SynergyMembers()
        {
            var (manager, _, _, summonerEffect) = SetupStandardIntegration();

            InvokeNonPublic(manager, "Start");

            // SummonerEffect 항목 보존
            Assert.That(manager.SynergyMembers.ContainsKey(summonerEffect), Is.True);
            // SummonTrait 항목 추가 — 분배에 등장한 trait 별로
            int summonTraitMemberEntries = 0;
            int totalSummonTraitMemberUnits = 0;
            foreach (var kv in manager.SynergyMembers)
            {
                if (kv.Key.SynergyType == SynergyType.SummonTrait)
                {
                    summonTraitMemberEntries++;
                    totalSummonTraitMemberUnits += kv.Value.Count;
                }
            }
            Assert.That(summonTraitMemberEntries, Is.GreaterThan(0),
                "SummonTrait 별 SynergyMembers 항목이 합산형으로 등록되어야 함");
            Assert.That(totalSummonTraitMemberUnits, Is.EqualTo(32),
                "SummonTrait 항목들의 unit 합계가 분배된 32 체와 일치해야 함");
        }

        [Test]
        public void Test_TC004_Start_Creates_SummonTraitSynergyController_Per_Trait()
        {
            var (manager, _, _, _) = SetupStandardIntegration();

            InvokeNonPublic(manager, "Start");

            var controllers = GetPrivateField<Dictionary<SynergyDefinitionData, SynergyController>>(manager, "_controllers");
            int summonTraitControllerCount = 0;
            foreach (var kv in controllers)
            {
                if (kv.Key.SynergyType == SynergyType.SummonTrait)
                {
                    Assert.That(kv.Value, Is.InstanceOf<SummonTraitSynergyController>(),
                        $"SummonTrait '{kv.Key.DisplayName}' 의 컨트롤러는 SummonTraitSynergyController 여야 함");
                    summonTraitControllerCount++;
                }
            }
            Assert.That(summonTraitControllerCount, Is.GreaterThan(0),
                "분배에 등장한 SummonTrait 마다 컨트롤러가 생성되어야 함");
        }

        // ── DoD-S3: GetSummonTrait public API ──

        [Test]
        public void Test_TC101_GetSummonTrait_Assigned_Unit_Returns_Assigned_Trait()
        {
            var (manager, summoners, _, _) = SetupStandardIntegration();
            InvokeNonPublic(manager, "Start");

            var traitMap = GetTraitMap(manager);
            var someUnit = summoners[0].Summoner.SummonPool[0];

            SynergyDefinitionData fetched = manager.GetSummonTrait(someUnit);

            Assert.That(fetched, Is.SameAs(traitMap[someUnit]));
        }

        [Test]
        public void Test_TC102_GetSummonTrait_Unassigned_Unit_Returns_Null()
        {
            var (manager, _, _, _) = SetupStandardIntegration();
            InvokeNonPublic(manager, "Start");
            var unrelatedUnit = CreateUnit(9999); // 분배에 포함되지 않은 unit

            Assert.That(manager.GetSummonTrait(unrelatedUnit), Is.Null);
        }

        [Test]
        public void Test_TC103_GetSummonTrait_Null_Logs_Error_And_Returns_Null()
        {
            var manager = CreateSynergyManagerInScene();

            LogAssert.Expect(LogType.Error, new Regex(@"\[SynergyManager\].*unit.*null"));
            var result = manager.GetSummonTrait(null);

            Assert.That(result, Is.Null);
        }

        // ── DoD-S4: 전장 종료 초기화 ──

        [Test]
        public void Test_TC201_OnBattleWin_Clears_SummonTraitMap()
        {
            var (manager, _, _, _) = SetupStandardIntegration();
            InvokeNonPublic(manager, "Start");
            InvokeNonPublic(manager, "OnEnable"); // EditMode 자동 비발화 보정 — Battle 이벤트 구독
            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(32), "전제 검증: Start 후 분배됨");

            GlobalEventBus.Publish(new OnBattleWinEventDto());

            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC202_OnBattleLose_Clears_SummonTraitMap()
        {
            var (manager, _, _, _) = SetupStandardIntegration();
            InvokeNonPublic(manager, "Start");
            InvokeNonPublic(manager, "OnEnable");
            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(32));

            GlobalEventBus.Publish(new OnBattleLoseEventDto());

            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC203_AfterOnDisable_BattleEvents_Do_Not_Clear_Map()
        {
            var (manager, _, _, _) = SetupStandardIntegration();
            InvokeNonPublic(manager, "Start");
            InvokeNonPublic(manager, "OnEnable");
            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(32));

            InvokeNonPublic(manager, "OnDisable");
            GlobalEventBus.Publish(new OnBattleWinEventDto());
            GlobalEventBus.Publish(new OnBattleLoseEventDto());

            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(32),
                "OnDisable 후에는 Battle 이벤트가 SummonTraitMap 을 비우지 않아야 함");
        }

        // ── DoD-S7: HandleDefenderChanged Spawn 결정 테이블 ──

        private SynergyManager SetupManagerWithDefenderMaps(
            Dictionary<UnitLoadOutData, SynergyDefinitionData> summonerEffectMap,
            Dictionary<UnitLoadOutData, SynergyDefinitionData> summonTraitMap)
        {
            var manager = CreateSynergyManagerInScene();
            var unitSummonerEffectMap = GetPrivateField<Dictionary<UnitLoadOutData, SynergyDefinitionData>>(manager, "_unitSummonerEffectMap");
            foreach (var kv in summonerEffectMap) unitSummonerEffectMap[kv.Key] = kv.Value;
            var traitMap = GetTraitMap(manager);
            foreach (var kv in summonTraitMap) traitMap[kv.Key] = kv.Value;
            return manager;
        }

        [Test]
        public void Test_TC301_DefenderSpawn_BothMaps_Adds_Both_Synergies()
        {
            var effect = CreateSummonerEffect();
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithDefenderMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, effect } },
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.HasSynergy(effect), Is.True);
            Assert.That(defender.HasSynergy(trait), Is.True);
            Assert.That(defender.Synergies.Count, Is.EqualTo(2));
        }

        [Test]
        public void Test_TC302_DefenderSpawn_OnlySummonerEffect_Adds_Only_That()
        {
            var effect = CreateSummonerEffect();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithDefenderMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, effect } },
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>());
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.HasSynergy(effect), Is.True);
            Assert.That(defender.Synergies.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_TC303_DefenderSpawn_OnlySummonTrait_Adds_Only_That()
        {
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithDefenderMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>(),
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.HasSynergy(trait), Is.True);
            Assert.That(defender.Synergies.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_TC304_DefenderSpawn_NoMaps_AddNoSynergies()
        {
            var unit = CreateUnit(1);
            var manager = SetupManagerWithDefenderMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>(),
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>());
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.Synergies.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC305_DefenderDespawn_HandlerIsNoOp()
        {
            var effect = CreateSummonerEffect();
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithDefenderMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, effect } },
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Despawn));

            Assert.That(defender.Synergies.Count, Is.EqualTo(0),
                "본 핸들러는 Spawn 만 처리, Despawn 은 SynergyController 책임");
        }

        // ── DoD-S2: 가드 (registry 미연결) ──

        [Test]
        public void Test_TC401_Start_With_Null_Registry_Logs_Error_And_Skips_SummonTrait()
        {
            CreateFactoryInScene();
            var summonerEffect = CreateSummonerEffect();
            var unit = CreateUnit(1);
            var summoner = CreateSummoner(new[] { unit }, summonerEffect);
            CreateSummonerManagerInScene(new[] { summoner });
            var manager = CreateSynergyManagerInScene(registry: null);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SynergyManager\].*summonTraitRegistry"));
            InvokeNonPublic(manager, "Start");

            // summonTraitMap 비어 있고 SummonTrait 활성화 없음. SummonerEffect 만 등록.
            Assert.That(GetTraitMap(manager).Count, Is.EqualTo(0));
            Assert.That(manager.SynergyActivations.ContainsKey(summonerEffect), Is.True);
            foreach (var kv in manager.SynergyActivations)
            {
                Assert.That(kv.Key.SynergyType, Is.Not.EqualTo(SynergyType.SummonTrait),
                    "registry 미연결 시 SummonTrait 활성화는 등록되지 않아야 함");
            }
        }

        // ── DoD-S7: Factory routing ──

        [Test]
        public void Test_TC501_Factory_Routes_SummonTraitType_To_SummonTraitSynergyController()
        {
            CreateFactoryInScene();
            var trait = CreateTrait();
            var activation = new SynergyActivation(trait);

            SynergyController controller = SynergyControllerFactory.Instance.Create(activation);

            Assert.That(controller, Is.InstanceOf<SummonTraitSynergyController>());
            controller.Dispose();
        }

        [Test]
        public void Test_TC502_Factory_Routes_SummonerEffect_To_Existing_Branch_NotSummonTraitController()
        {
            CreateFactoryInScene();
            var effect = CreateSummonerEffect(SynergyId.Bruiser);
            var activation = new SynergyActivation(effect);

            SynergyController controller = SynergyControllerFactory.Instance.Create(activation);

            Assert.That(controller, Is.Not.InstanceOf<SummonTraitSynergyController>());
            Assert.That(controller, Is.InstanceOf<BruiserSynergyController>(),
                "SynergyId.Bruiser → BruiserSynergyController 기존 분기 유지");
            controller.Dispose();
        }

        // ── DoD-S7: NoOpSynergyStatusEffect ──

        [Test]
        public void Test_TC601_SummonTraitSynergyController_CreateSynergyStatusEffect_Returns_NoOp()
        {
            CreateFactoryInScene();
            var trait = CreateTrait();
            var seDefinition = ScriptableObject.CreateInstance<StatusEffectDefinitionData>();
            _createdAssets.Add(seDefinition);
            SetPrivateField(trait, "statusEffectDefinition", seDefinition);
            var activation = new SynergyActivation(trait);

            var controller = new SummonTraitSynergyController(activation);
            var sse = InvokeNonPublic(controller, "CreateSynergyStatusEffect");

            Assert.That(sse, Is.InstanceOf<NoOpSynergyStatusEffect>());
            controller.Dispose();
        }

        /// <summary>테스트용 Defender stub.</summary>
        private class StubDefender : Defender { }
    }
}
