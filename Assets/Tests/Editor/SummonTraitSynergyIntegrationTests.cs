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
    public class SummonTraitSynergyIntegrationTests
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
            ResetSceneSingletonInstance<SummonTraitStore>();
            ResetSceneSingletonInstance<SummonerManager>();
            ResetSceneSingletonInstance<SynergyManager>();
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

        /// <summary>비공개 필드 값을 읽는다.</summary>
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
        private SummonerLoadOutData CreateSummoner(UnitLoadOutData[] summonPool, SynergyDefinitionData summonerEffect = null)
        {
            var summonerDef = ScriptableObject.CreateInstance<SummonerDefinitionData>();
            _createdAssets.Add(summonerDef);
            SetPrivateField(summonerDef, "summonPool", summonPool);
            if (summonerEffect != null)
            {
                SetPrivateField(summonerDef, "summonerEffect", summonerEffect);
            }

            var summonerLoadOut = ScriptableObject.CreateInstance<SummonerLoadOutData>();
            _createdAssets.Add(summonerLoadOut);
            SetPrivateField(summonerLoadOut, "summoner", summonerDef);
            return summonerLoadOut;
        }

        /// <summary>SummonTraitRegistry SO 생성.</summary>
        private SummonTraitRegistry CreateRegistry(SynergyDefinitionData[] traits)
        {
            var registry = ScriptableObject.CreateInstance<SummonTraitRegistry>();
            _createdAssets.Add(registry);
            SetPrivateField(registry, "traits", traits ?? new SynergyDefinitionData[0]);
            return registry;
        }

        /// <summary>새 GameObject 에 SummonTraitStore 부착 + Initialize(map) + 정적 인스턴스 등록.</summary>
        private SummonTraitStore CreateStoreInScene(Dictionary<UnitLoadOutData, SynergyDefinitionData> initialMap = null)
        {
            var go = new GameObject("TestSummonTraitStore");
            _createdGameObjects.Add(go);
            var store = go.AddComponent<SummonTraitStore>();
            SetSceneSingletonInstance(store);
            if (initialMap != null)
            {
                store.Initialize(initialMap);
            }
            return store;
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

        /// <summary>SynergyControllerFactory 인스턴스 생성. arcanistSpellPowerDefinition 은 SummonTrait 경로에서 사용 안 함.</summary>
        private SynergyControllerFactory CreateFactoryInScene()
        {
            var go = new GameObject("TestSynergyControllerFactory");
            _createdGameObjects.Add(go);
            var factory = go.AddComponent<SynergyControllerFactory>();
            SetSceneSingletonInstance(factory);
            return factory;
        }

        /// <summary>SynergyManager 인스턴스 생성 (Start 자동 발화 안 됨).</summary>
        private SynergyManager CreateSynergyManagerInScene()
        {
            var go = new GameObject("TestSynergyManager");
            _createdGameObjects.Add(go);
            var manager = go.AddComponent<SynergyManager>();
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

        // ── Distributor 정상 분배·이벤트 카운트 셋업 ──

        /// <summary>정상 분배 경로 셋업: registry + SummonerManager + Store + Distributor 모두 준비.</summary>
        private SummonTraitDistributor SetupNormalDistributor()
        {
            var trait = CreateTrait();
            var registry = CreateRegistry(new[] { trait });
            var unit = CreateUnit(1);
            var summoner = CreateSummoner(new[] { unit }, CreateSummonerEffect());
            CreateSummonerManagerInScene(new[] { summoner });
            CreateStoreInScene();

            var go = new GameObject("TestSummonTraitDistributor");
            _createdGameObjects.Add(go);
            var distributor = go.AddComponent<SummonTraitDistributor>();
            SetPrivateField(distributor, "registry", registry);
            return distributor;
        }

        // ── DoD-S7: Factory routing ──

        [Test]
        public void Test_TC001_Factory_Routes_SummonTraitType_To_SummonTraitSynergyController()
        {
            CreateFactoryInScene();
            var trait = CreateTrait();
            var activation = new SynergyActivation(trait);

            SynergyController controller = SynergyControllerFactory.Instance.Create(activation);

            Assert.That(controller, Is.InstanceOf<SummonTraitSynergyController>());
            controller.Dispose();
        }

        [Test]
        public void Test_TC002_Factory_Routes_SummonerEffect_To_Existing_SwitchBranch_NotSummonTraitController()
        {
            CreateFactoryInScene();
            var effect = CreateSummonerEffect(SynergyId.Bruiser);
            var activation = new SynergyActivation(effect);

            SynergyController controller = SynergyControllerFactory.Instance.Create(activation);

            Assert.That(controller, Is.Not.InstanceOf<SummonTraitSynergyController>(),
                "SummonerEffect 타입은 SummonTrait 통합 라우팅을 거치지 않아야 함 (회귀 방지)");
            Assert.That(controller, Is.InstanceOf<BruiserSynergyController>(),
                "SynergyId.Bruiser → BruiserSynergyController 기존 분기");
            controller.Dispose();
        }

        // ── DoD-S7: NoOp SSE ──

        [Test]
        public void Test_TC101_SummonTraitSynergyController_CreateSynergyStatusEffect_Returns_NoOpSynergyStatusEffect()
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

        // ── DoD-S7: Distributor 분배 완료 이벤트 ──

        [Test]
        public void Test_TC201_Distributor_NormalPath_Publishes_OnSummonTraitsDistributedEventDto_Once()
        {
            var distributor = SetupNormalDistributor();
            int publishedCount = 0;
            void Handler(OnSummonTraitsDistributedEventDto _) => publishedCount++;
            GlobalEventBus.Subscribe<OnSummonTraitsDistributedEventDto>(Handler);

            try
            {
                InvokeNonPublic(distributor, "OnBattleStart", new OnBattleStartEventDto());
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnSummonTraitsDistributedEventDto>(Handler);
            }

            Assert.That(publishedCount, Is.EqualTo(1),
                "정상 분배 경로에서 OnSummonTraitsDistributedEventDto 가 정확히 1 회 발행되어야 함");
        }

        [TestCase("registry_null", @"\[SummonTraitDistributor\].*registry")]
        [TestCase("summoner_manager_null", @"\[SummonTraitDistributor\].*SummonerManager")]
        [TestCase("store_null", @"\[SummonTraitDistributor\].*SummonTraitStore")]
        public void Test_TC202_Distributor_Guard_Failure_Does_Not_Publish_DistributedEvent(string scenario, string expectedLogPattern)
        {
            var trait = CreateTrait();
            var registry = CreateRegistry(new[] { trait });
            var unit = CreateUnit(1);
            var summoner = CreateSummoner(new[] { unit }, CreateSummonerEffect());

            switch (scenario)
            {
                case "registry_null":
                    CreateSummonerManagerInScene(new[] { summoner });
                    CreateStoreInScene();
                    break;
                case "summoner_manager_null":
                    CreateStoreInScene();
                    // SummonerManager 미생성
                    break;
                case "store_null":
                    CreateSummonerManagerInScene(new[] { summoner });
                    // Store 미생성
                    break;
            }

            var go = new GameObject("TestSummonTraitDistributor");
            _createdGameObjects.Add(go);
            var distributor = go.AddComponent<SummonTraitDistributor>();
            if (scenario != "registry_null")
            {
                SetPrivateField(distributor, "registry", registry);
            }

            int publishedCount = 0;
            void Handler(OnSummonTraitsDistributedEventDto _) => publishedCount++;
            GlobalEventBus.Subscribe<OnSummonTraitsDistributedEventDto>(Handler);

            try
            {
                LogAssert.Expect(LogType.Error, new Regex(expectedLogPattern));
                InvokeNonPublic(distributor, "OnBattleStart", new OnBattleStartEventDto());
            }
            finally
            {
                GlobalEventBus.Unsubscribe<OnSummonTraitsDistributedEventDto>(Handler);
            }

            Assert.That(publishedCount, Is.EqualTo(0),
                $"{scenario} 가드 실패 시 OnSummonTraitsDistributedEventDto 가 발행되어서는 안 됨");
        }

        // ── DoD-S5·S6·S7: SynergyManager.HandleSummonTraitsDistributed ──

        [Test]
        public void Test_TC301_HandleDistributed_Registers_Unique_Traits_In_SynergyActivations()
        {
            CreateFactoryInScene();
            var traitA = CreateTrait("TraitA");
            var traitB = CreateTrait("TraitB");
            var traitC = CreateTrait("TraitC");
            var unit1 = CreateUnit(1);
            var unit2 = CreateUnit(2);
            var unit3 = CreateUnit(3);
            var unit4 = CreateUnit(4);
            // 4 unit / 3 unique trait — A 가 두 unit 에 배정
            CreateStoreInScene(new Dictionary<UnitLoadOutData, SynergyDefinitionData>
            {
                { unit1, traitA }, { unit2, traitA }, { unit3, traitB }, { unit4, traitC }
            });
            var manager = CreateSynergyManagerInScene();

            InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto());

            Assert.That(manager.SynergyActivations.ContainsKey(traitA), Is.True);
            Assert.That(manager.SynergyActivations.ContainsKey(traitB), Is.True);
            Assert.That(manager.SynergyActivations.ContainsKey(traitC), Is.True);
            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(3),
                "unique trait 수만큼 활성화가 등록되어야 함");
        }

        [Test]
        public void Test_TC302_HandleDistributed_Builds_SynergyMembers_With_Owning_Units()
        {
            CreateFactoryInScene();
            var traitA = CreateTrait("TraitA");
            var traitB = CreateTrait("TraitB");
            var unit1 = CreateUnit(1);
            var unit2 = CreateUnit(2);
            var unit3 = CreateUnit(3);
            CreateStoreInScene(new Dictionary<UnitLoadOutData, SynergyDefinitionData>
            {
                { unit1, traitA }, { unit2, traitA }, { unit3, traitB }
            });
            var manager = CreateSynergyManagerInScene();

            InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto());

            Assert.That(manager.SynergyMembers[traitA], Has.Member(unit1));
            Assert.That(manager.SynergyMembers[traitA], Has.Member(unit2));
            Assert.That(manager.SynergyMembers[traitA].Count, Is.EqualTo(2));
            Assert.That(manager.SynergyMembers[traitB], Has.Member(unit3));
            Assert.That(manager.SynergyMembers[traitB].Count, Is.EqualTo(1));
        }

        // TC-303 제거 (재진입 — 핫픽스 2e5eb39 후): "trait 별 OnSynergyRecalculatedEventDto 발행" 은
        // UR/SR DoD 가 아니라 과거 구현 메커니즘이었음. UI 갱신 보장은 SynergyListPanel.Start 의
        // _synergyActivations 자체 폴링으로 충족 — 불일치 보고서 MIS-R1 참조.

        [Test]
        public void Test_TC304_HandleDistributed_Coexists_SummonerEffect_And_SummonTrait_Activations()
        {
            CreateFactoryInScene();
            var effect = CreateSummonerEffect(SynergyId.Bruiser, "BruiserEffect");
            var trait = CreateTrait("TraitX");
            var unit1 = CreateUnit(1);

            // SummonerEffect 활성화를 미리 채워 둠 (BuildUnitSynergyMap·InitializeSynergyActivations 결과 모사)
            var manager = CreateSynergyManagerInScene();
            var activations = GetPrivateField<Dictionary<SynergyDefinitionData, SynergyActivation>>(manager, "_synergyActivations");
            activations[effect] = new SynergyActivation(effect);

            CreateStoreInScene(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit1, trait } });

            InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto());

            Assert.That(manager.SynergyActivations.ContainsKey(effect), Is.True,
                "기존 SummonerEffect 활성화는 보존되어야 함");
            Assert.That(manager.SynergyActivations.ContainsKey(trait), Is.True,
                "SummonTrait 활성화도 같은 사전에 등록되어야 함");
            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(2));
        }

        [Test]
        public void Test_TC305_HandleDistributed_Preserves_SummonerEffect_Members_While_Adding_SummonTrait()
        {
            CreateFactoryInScene();
            var effect = CreateSummonerEffect(SynergyId.Bruiser);
            var trait = CreateTrait("TraitX");
            var unit1 = CreateUnit(1);
            var unit2 = CreateUnit(2);

            // SummonerEffect 의 SynergyMembers 항목을 미리 셋업
            var manager = CreateSynergyManagerInScene();
            var members = GetPrivateField<Dictionary<SynergyDefinitionData, List<UnitLoadOutData>>>(manager, "_synergyMembers");
            members[effect] = new List<UnitLoadOutData> { unit1, unit2 };
            // public 뷰도 갱신 (실제 SynergyManager.Start 가 BuildSynergyMembersMap 에서 하는 일)
            var publicView = new Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>>();
            foreach (var kv in members) publicView[kv.Key] = kv.Value;
            SetPrivateField(manager, "_synergyMembersPublic", publicView);

            CreateStoreInScene(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit1, trait } });

            InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto());

            // SummonerEffect 멤버 보존
            Assert.That(manager.SynergyMembers[effect], Has.Member(unit1));
            Assert.That(manager.SynergyMembers[effect], Has.Member(unit2));
            // SummonTrait 멤버 추가
            Assert.That(manager.SynergyMembers[trait], Has.Member(unit1));
        }

        [Test]
        public void Test_TC306_HandleDistributed_StoreInstance_Null_Logs_Error_And_Skips()
        {
            CreateFactoryInScene();
            // Store 미생성 → SummonTraitStore.Instance == null
            var manager = CreateSynergyManagerInScene();

            LogAssert.Expect(LogType.Error, new Regex(@"\[SynergyManager\].*SummonTraitStore"));
            InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto());

            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC307_HandleDistributed_Empty_Store_Is_NoOp()
        {
            CreateFactoryInScene();
            CreateStoreInScene(); // 빈 상태
            var manager = CreateSynergyManagerInScene();

            Assert.DoesNotThrow(() => InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto()));

            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(0));
            Assert.That(manager.SynergyMembers.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC308_HandleDistributed_Duplicate_Trait_Logs_Warning_And_Keeps_Existing()
        {
            CreateFactoryInScene();
            var trait = CreateTrait("DupTrait");
            var unit1 = CreateUnit(1);

            // 기존 활성화 미리 등록
            var manager = CreateSynergyManagerInScene();
            var existingActivation = new SynergyActivation(trait);
            var activations = GetPrivateField<Dictionary<SynergyDefinitionData, SynergyActivation>>(manager, "_synergyActivations");
            activations[trait] = existingActivation;

            CreateStoreInScene(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit1, trait } });

            LogAssert.Expect(LogType.Warning, new Regex(@"\[SynergyManager\].*SummonTrait.*이미 등록"));
            InvokeNonPublic(manager, "HandleSummonTraitsDistributed", new OnSummonTraitsDistributedEventDto());

            Assert.That(manager.SynergyActivations[trait], Is.SameAs(existingActivation),
                "기존 활성화 인스턴스가 그대로 유지되어야 함");
        }

        // ── DoD-S7: HandleDefenderChanged Spawn 처리 ──

        private SynergyManager SetupManagerWithMaps(
            Dictionary<UnitLoadOutData, SynergyDefinitionData> summonerEffectMap,
            Dictionary<UnitLoadOutData, SynergyDefinitionData> summonTraitMap)
        {
            var manager = CreateSynergyManagerInScene();
            var unitSynergyMap = GetPrivateField<Dictionary<UnitLoadOutData, SynergyDefinitionData>>(manager, "_unitSynergyMap");
            foreach (var kv in summonerEffectMap) unitSynergyMap[kv.Key] = kv.Value;
            var unitSummonTraitMap = GetPrivateField<Dictionary<UnitLoadOutData, SynergyDefinitionData>>(manager, "_unitSummonTraitMap");
            foreach (var kv in summonTraitMap) unitSummonTraitMap[kv.Key] = kv.Value;
            return manager;
        }

        [Test]
        public void Test_TC401_DefenderSpawn_Both_Maps_Have_Unit_Adds_Both_Synergies()
        {
            var effect = CreateSummonerEffect();
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithMaps(
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
        public void Test_TC402_DefenderSpawn_OnlySummonerEffect_Adds_Only_That()
        {
            var effect = CreateSummonerEffect();
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, effect } },
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>());
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.HasSynergy(effect), Is.True);
            Assert.That(defender.HasSynergy(trait), Is.False);
            Assert.That(defender.Synergies.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_TC403_DefenderSpawn_OnlySummonTrait_Adds_Only_That()
        {
            var effect = CreateSummonerEffect();
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>(),
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.HasSynergy(trait), Is.True);
            Assert.That(defender.HasSynergy(effect), Is.False);
            Assert.That(defender.Synergies.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_TC404_DefenderSpawn_No_Maps_Add_No_Synergies()
        {
            var unit = CreateUnit(1);
            var manager = SetupManagerWithMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>(),
                new Dictionary<UnitLoadOutData, SynergyDefinitionData>());
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

            Assert.That(defender.Synergies.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC405_DefenderDespawn_Handler_Is_NoOp_For_This_Integration()
        {
            var effect = CreateSummonerEffect();
            var trait = CreateTrait();
            var unit = CreateUnit(1);
            var manager = SetupManagerWithMaps(
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, effect } },
                new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            var defender = CreateStubDefender(unit);

            InvokeNonPublic(manager, "HandleDefenderChanged",
                new OnDefenderChangedEventDto(defender, DefenderChanges.Despawn));

            Assert.That(defender.Synergies.Count, Is.EqualTo(0),
                "본 통합 핸들러는 Spawn 만 처리, Despawn 은 SynergyController 측 책임");
        }

        // ── DoD-S5·S6·S7: OQ-9 Start() 타이밍 경합 보정 ──

        [Test]
        public void Test_TC501_Start_With_Empty_Store_Does_Not_Invoke_Handler_Until_Event_Published()
        {
            CreateFactoryInScene();
            var effect = CreateSummonerEffect();
            var unit = CreateUnit(1);
            var summoner = CreateSummoner(new[] { unit }, effect);
            CreateSummonerManagerInScene(new[] { summoner });
            CreateStoreInScene(); // 빈 Store
            var manager = CreateSynergyManagerInScene();

            InvokeNonPublic(manager, "Start");

            // 빈 Store 라 사후 동기 검사로 핸들러 미호출 — SynergyActivations 에는 SummonerEffect 만
            Assert.That(manager.SynergyActivations.ContainsKey(effect), Is.True);
            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(1),
                "빈 Store 상태로 Start → SummonerEffect 만 등록, SummonTrait 추가 없음");

            // 이후 분배 이벤트 발행 → 구독되어 있어 처리됨
            var trait = CreateTrait("LateTrait");
            SummonTraitStore.Instance.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            GlobalEventBus.Publish(new OnSummonTraitsDistributedEventDto());

            Assert.That(manager.SynergyActivations.ContainsKey(trait), Is.True);
            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(2));
        }

        [Test]
        public void Test_TC502_Start_With_Prefilled_Store_Sync_Invokes_Handler_Once()
        {
            CreateFactoryInScene();
            var effect = CreateSummonerEffect();
            var trait = CreateTrait("EarlyTrait");
            var unit = CreateUnit(1);
            var summoner = CreateSummoner(new[] { unit }, effect);
            CreateSummonerManagerInScene(new[] { summoner });
            // Store 에 미리 분배 결과 셋업 (RoundManager.Start 가 SynergyManager.Start 보다 먼저 실행된 시나리오)
            CreateStoreInScene(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });
            var manager = CreateSynergyManagerInScene();

            InvokeNonPublic(manager, "Start");

            // 사후 동기 검사로 핸들러 1회 호출됨 — SummonTrait 도 활성화에 등록
            Assert.That(manager.SynergyActivations.ContainsKey(effect), Is.True);
            Assert.That(manager.SynergyActivations.ContainsKey(trait), Is.True,
                "Start 시점 Store 에 데이터가 있으면 사후 동기 검사로 핸들러가 호출되어야 함 (OQ-9)");
            Assert.That(manager.SynergyActivations.Count, Is.EqualTo(2));
        }

        /// <summary>테스트용 Defender stub — 기존 SynergyControllerTests 패턴 차용.</summary>
        private class StubDefender : Defender { }
    }
}
