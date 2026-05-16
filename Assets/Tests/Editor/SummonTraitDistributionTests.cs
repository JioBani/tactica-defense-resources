using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Common.Data.Summoners.SummonerDefinitions;
using Common.Data.Summoners.SummonerFormations;
using Common.Data.Summoners.SummonerLoadOuts;
using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using NUnit.Framework;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.SummonTrait;
using Scenes.Battle.Feature.Unit.Summoners;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SummonTraitDistributionTests
    {
        private readonly List<ScriptableObject> _createdAssets = new();
        private readonly List<GameObject> _createdGameObjects = new();

        [SetUp]
        public void SetUp()
        {
            ResetSceneSingletonInstance<SummonTraitStore>();
            ResetSceneSingletonInstance<SummonerManager>();
            ResetGlobalEventBus();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _createdGameObjects.Clear();

            foreach (var asset in _createdAssets)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }
            _createdAssets.Clear();

            ResetSceneSingletonInstance<SummonTraitStore>();
            ResetSceneSingletonInstance<SummonerManager>();
            ResetGlobalEventBus();
        }

        // ── helpers ──

        /// <summary>EditMode 에서 자동 발화하지 않는 MonoBehaviour 비공개 라이프사이클 메서드를 reflection 으로 호출한다.</summary>
        private static void InvokeNonPublic(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(target, args);
        }

        /// <summary>비공개 [SerializeField] 필드에 reflection 으로 값을 주입한다.</summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        /// <summary>SceneSingleton 의 정적 인스턴스를 reflection 으로 설정한다.</summary>
        private static void SetSceneSingletonInstance<T>(T instance) where T : Component
        {
            var instanceField = typeof(SceneSingleton<T>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, instance);
        }

        /// <summary>SceneSingleton 의 정적 인스턴스 캐시를 비워 테스트 간 격리를 보장한다.</summary>
        private static void ResetSceneSingletonInstance<T>() where T : Component
        {
            var instanceField = typeof(SceneSingleton<T>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }

        /// <summary>GlobalEventBus 의 정적 Handlers 사전을 reflection 으로 반환한다. 구독 등록 여부 검증과 리셋에 공통 사용된다.</summary>
        private static IDictionary GetEventBusHandlers()
        {
            var handlersField = typeof(GlobalEventBus)
                .GetField("Handlers", BindingFlags.NonPublic | BindingFlags.Static);
            return handlersField?.GetValue(null) as IDictionary;
        }

        /// <summary>GlobalEventBus 의 정적 핸들러 사전을 비워 이전 테스트의 구독이 누수되지 않도록 보장한다.</summary>
        private static void ResetGlobalEventBus()
        {
            GetEventBusHandlers()?.Clear();
        }

        /// <summary>테스트용 UnitLoadOutData 를 in-memory 로 생성하고 TearDown 자동 정리 대상에 등록한다.</summary>
        private UnitLoadOutData CreateUnit()
        {
            var so = ScriptableObject.CreateInstance<UnitLoadOutData>();
            _createdAssets.Add(so);
            return so;
        }

        /// <summary>테스트용 SynergyDefinitionData(특성) 를 in-memory 로 생성하고 TearDown 자동 정리 대상에 등록한다.</summary>
        private SynergyDefinitionData CreateTrait()
        {
            var so = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(so);
            return so;
        }

        /// <summary>지정된 unit 풀을 가진 SummonerLoadOutData 를 in-memory 로 생성한다. unitsInPool 이 null 이면 SummonPool 미설정.</summary>
        private SummonerLoadOutData CreateSummoner(UnitLoadOutData[] unitsInPool)
        {
            var summonerDefinition = ScriptableObject.CreateInstance<SummonerDefinitionData>();
            _createdAssets.Add(summonerDefinition);
            if (unitsInPool != null)
            {
                SetPrivateField(summonerDefinition, "summonPool", unitsInPool);
            }

            var summonerLoadOut = ScriptableObject.CreateInstance<SummonerLoadOutData>();
            _createdAssets.Add(summonerLoadOut);
            SetPrivateField(summonerLoadOut, "summoner", summonerDefinition);
            return summonerLoadOut;
        }

        /// <summary>기본 8 체 풀을 가진 정상 SummonerLoadOutData 를 생성한다.</summary>
        private SummonerLoadOutData CreateNormalSummoner(int poolSize = 8)
        {
            var pool = new UnitLoadOutData[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                pool[i] = CreateUnit();
            }
            return CreateSummoner(pool);
        }

        /// <summary>SummonTraitRegistry SO 를 생성하고 지정된 trait 배열을 주입한다.</summary>
        private SummonTraitRegistry CreateRegistry(SynergyDefinitionData[] traits)
        {
            var registry = ScriptableObject.CreateInstance<SummonTraitRegistry>();
            _createdAssets.Add(registry);
            if (traits != null)
            {
                SetPrivateField(registry, "traits", traits);
            }
            return registry;
        }

        /// <summary>새 GameObject 에 SummonTraitStore 를 부착하고 정적 인스턴스로 등록한 뒤 OnEnable 까지 발화시킨다.</summary>
        private SummonTraitStore CreateStoreInScene()
        {
            var go = new GameObject("TestSummonTraitStore");
            _createdGameObjects.Add(go);
            var store = go.AddComponent<SummonTraitStore>();
            SetSceneSingletonInstance(store);
            InvokeNonPublic(store, "OnEnable");
            return store;
        }

        /// <summary>새 GameObject 에 SummonerManager 를 부착하고 편성 데이터를 주입한 뒤 정적 인스턴스로 등록한다.</summary>
        private SummonerManager CreateSummonerManagerInScene(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            var go = new GameObject("TestSummonerManager");
            _createdGameObjects.Add(go);
            var manager = go.AddComponent<SummonerManager>();
            manager.SetFormation(new SummonerFormation(summoners));
            SetSceneSingletonInstance(manager);
            return manager;
        }

        /// <summary>새 GameObject 에 SummonTraitDistributor 를 부착하고 registry 주입 + OnEnable 발화까지 수행한다.</summary>
        private (GameObject go, SummonTraitDistributor distributor) CreateDistributorInScene(SummonTraitRegistry registry)
        {
            var go = new GameObject("TestSummonTraitDistributor");
            _createdGameObjects.Add(go);
            var distributor = go.AddComponent<SummonTraitDistributor>();
            if (registry != null)
            {
                SetPrivateField(distributor, "registry", registry);
            }
            InvokeNonPublic(distributor, "OnEnable");
            return (go, distributor);
        }

        // ── DoD-S1: Registry — 특성 풀 인식 ──

        [Test]
        public void Test_TC001_Registry_Traits_Exposes_Inspector_Array_AsIs()
        {
            var trait1 = CreateTrait();
            var trait2 = CreateTrait();
            var trait3 = CreateTrait();
            var registry = CreateRegistry(new[] { trait1, trait2, trait3 });

            var exposed = registry.Traits;

            Assert.That(exposed.Count, Is.EqualTo(3));
            Assert.That(exposed[0], Is.SameAs(trait1));
            Assert.That(exposed[1], Is.SameAs(trait2));
            Assert.That(exposed[2], Is.SameAs(trait3));
        }

        [Test]
        public void Test_TC002_Empty_Registry_Has_Zero_Traits()
        {
            var registry = CreateRegistry(traits: null); // 기본값 빈 배열 유지

            Assert.That(registry.Traits, Is.Not.Null);
            Assert.That(registry.Traits.Count, Is.EqualTo(0));
        }

        // ── DoD-S2: Service — 균등 랜덤 분배 ──

        [Test]
        public void Test_TC101_Distribute_32_Units_All_Assigned()
        {
            var summoners = new[]
            {
                CreateNormalSummoner(), CreateNormalSummoner(),
                CreateNormalSummoner(), CreateNormalSummoner()
            };
            var traitPool = new[] { CreateTrait(), CreateTrait(), CreateTrait(), CreateTrait(), CreateTrait() };
            var service = new SummonTraitService();

            var result = service.Distribute(summoners, traitPool);

            Assert.That(result.Count, Is.EqualTo(32), "4 소환술사 × 8 체 = 32 entries 모두 배정되어야 함");
            // 모든 unit 이 키로 포함되어 있는지 검증
            foreach (var summoner in summoners)
            {
                foreach (var unit in summoner.Summoner.SummonPool)
                {
                    Assert.That(result.ContainsKey(unit), Is.True, $"unit {unit.name} 이 배정되어야 함");
                }
            }
        }

        [Test]
        public void Test_TC102_Distribute_Assigned_Traits_All_Belong_To_Pool()
        {
            var summoners = new[]
            {
                CreateNormalSummoner(), CreateNormalSummoner(),
                CreateNormalSummoner(), CreateNormalSummoner()
            };
            var traitPool = new[] { CreateTrait(), CreateTrait(), CreateTrait(), CreateTrait(), CreateTrait() };
            var poolSet = new HashSet<SynergyDefinitionData>(traitPool);
            var service = new SummonTraitService();

            var result = service.Distribute(summoners, traitPool);

            foreach (var kvp in result)
            {
                Assert.That(poolSet.Contains(kvp.Value), Is.True,
                    $"배정된 trait 는 traitPool 안의 멤버여야 함 — unit {kvp.Key.name} 에 풀 외 trait 가 배정됨");
            }
        }

        [Test]
        public void Test_TC103_Distribute_Null_Pool_Logs_Error_And_Returns_Empty()
        {
            var summoners = new[] { CreateNormalSummoner() };
            var service = new SummonTraitService();

            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitService\].*풀.*비어"));
            var result = service.Distribute(summoners, traitPool: null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC104_Distribute_Empty_Pool_Logs_Error_And_Returns_Empty()
        {
            var summoners = new[] { CreateNormalSummoner() };
            var service = new SummonTraitService();

            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitService\].*풀.*비어"));
            var result = service.Distribute(summoners, traitPool: new SynergyDefinitionData[0]);

            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC105_Distribute_Null_Summoner_Item_Is_Skipped()
        {
            var validSummoners = new[]
            {
                CreateNormalSummoner(), CreateNormalSummoner(), CreateNormalSummoner()
            };
            var summoners = new SummonerLoadOutData[] { validSummoners[0], null, validSummoners[1], validSummoners[2] };
            var traitPool = new[] { CreateTrait() };
            var service = new SummonTraitService();

            var result = service.Distribute(summoners, traitPool);

            Assert.That(result.Count, Is.EqualTo(24), "3 valid × 8 체 = 24 entries, null 항목은 스킵");
        }

        [Test]
        public void Test_TC106_Distribute_Empty_SummonPool_Logs_Warning_And_Skips()
        {
            var emptyPoolSummoner = CreateSummoner(new UnitLoadOutData[0]);
            var normalSummoner = CreateNormalSummoner();
            var summoners = new[] { emptyPoolSummoner, normalSummoner };
            var traitPool = new[] { CreateTrait() };
            var service = new SummonTraitService();

            LogAssert.Expect(LogType.Warning, new Regex(@"\[SummonTraitService\].*SummonPool.*비어있"));
            var result = service.Distribute(summoners, traitPool);

            Assert.That(result.Count, Is.EqualTo(8), "정상 소환술사 1 명만 배정 (빈 풀은 스킵)");
        }

        [Test]
        public void Test_TC107_Distribute_Null_Unit_In_Pool_Logs_Warning_And_Skips()
        {
            var poolWithNull = new UnitLoadOutData[]
            {
                CreateUnit(), null, CreateUnit(), CreateUnit(),
                CreateUnit(), CreateUnit(), CreateUnit(), CreateUnit()
            };
            var summoner = CreateSummoner(poolWithNull);
            var traitPool = new[] { CreateTrait() };
            var service = new SummonTraitService();

            LogAssert.Expect(LogType.Warning, new Regex(@"\[SummonTraitService\].*null UnitLoadOutData"));
            var result = service.Distribute(new[] { summoner }, traitPool);

            Assert.That(result.Count, Is.EqualTo(7), "8 - 1 null = 7 entries");
        }

        [Test]
        public void Test_TC108_Distribute_Single_Trait_Pool_Assigns_That_Trait_To_All_Units()
        {
            var summoner = CreateNormalSummoner();
            var onlyTrait = CreateTrait();
            var service = new SummonTraitService();

            var result = service.Distribute(new[] { summoner }, new[] { onlyTrait });

            Assert.That(result.Count, Is.EqualTo(8));
            foreach (var kvp in result)
            {
                Assert.That(kvp.Value, Is.SameAs(onlyTrait), "단일 trait 풀이면 모든 unit 이 그 trait 로 배정");
            }
        }

        // ── DoD-S1·S2: Distributor — 라이프사이클 + 통합 ──

        [Test]
        public void Test_TC201_OnEnable_Subscribes_To_OnBattleStartEventDto()
        {
            var registry = CreateRegistry(new[] { CreateTrait() });
            CreateDistributorInScene(registry);

            Assert.That(GetEventBusHandlers().Contains(typeof(OnBattleStartEventDto)), Is.True,
                "OnEnable 이후 GlobalEventBus.Handlers 에 OnBattleStartEventDto 구독이 등록되어야 함");
        }

        [Test]
        public void Test_TC202_OnDisable_Unsubscribes_From_OnBattleStartEventDto()
        {
            var registry = CreateRegistry(new[] { CreateTrait() });
            var (_, distributor) = CreateDistributorInScene(registry);

            InvokeNonPublic(distributor, "OnDisable");

            Assert.That(GetEventBusHandlers().Contains(typeof(OnBattleStartEventDto)), Is.False,
                "OnDisable 이후 구독이 해제되어야 함");
        }

        [Test]
        public void Test_TC203_Happy_Path_Event_Triggers_Store_Initialize()
        {
            var summoners = new[]
            {
                CreateNormalSummoner(), CreateNormalSummoner(),
                CreateNormalSummoner(), CreateNormalSummoner()
            };
            var traits = new[] { CreateTrait(), CreateTrait(), CreateTrait() };
            var registry = CreateRegistry(traits);
            CreateSummonerManagerInScene(summoners);
            var store = CreateStoreInScene();
            CreateDistributorInScene(registry);

            GlobalEventBus.Publish(new OnBattleStartEventDto());

            Assert.That(store.All.Count, Is.EqualTo(32),
                "정상 경로에서 4 × 8 = 32 entries 가 Store 에 저장되어야 함");
            // 배정된 trait 들이 모두 풀의 멤버인지도 확인 (분배 결과의 정합성)
            var poolSet = new HashSet<SynergyDefinitionData>(traits);
            foreach (var kvp in store.All)
            {
                Assert.That(poolSet.Contains(kvp.Value), Is.True);
            }
        }

        [Test]
        public void Test_TC204_Registry_Not_Wired_Logs_Error_And_Skips()
        {
            CreateSummonerManagerInScene(new[] { CreateNormalSummoner() });
            var store = CreateStoreInScene();
            CreateDistributorInScene(registry: null);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitDistributor\].*registry"));
            GlobalEventBus.Publish(new OnBattleStartEventDto());

            Assert.That(store.All.Count, Is.EqualTo(0), "registry 미연결 시 Store 는 변경되지 않아야 함");
        }

        [Test]
        public void Test_TC205_SummonerManager_Missing_Logs_Error_And_Skips()
        {
            var registry = CreateRegistry(new[] { CreateTrait() });
            var store = CreateStoreInScene();
            // SummonerManager 인스턴스 미생성
            CreateDistributorInScene(registry);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitDistributor\].*SummonerManager"));
            GlobalEventBus.Publish(new OnBattleStartEventDto());

            Assert.That(store.All.Count, Is.EqualTo(0), "SummonerManager 부재 시 Store 는 변경되지 않아야 함");
        }

        [Test]
        public void Test_TC206_SummonTraitStore_Missing_Logs_Error_And_Skips()
        {
            var registry = CreateRegistry(new[] { CreateTrait() });
            CreateSummonerManagerInScene(new[] { CreateNormalSummoner() });
            // SummonTraitStore 인스턴스 미생성
            CreateDistributorInScene(registry);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitDistributor\].*SummonTraitStore"));
            GlobalEventBus.Publish(new OnBattleStartEventDto());

            // Store 가 없으므로 분배 결과는 어디에도 저장되지 않는다 — LogError 발생만으로 충분 (LogAssert.Expect 가 검증).
        }
    }
}
