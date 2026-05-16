using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using NUnit.Framework;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.SummonTrait;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SummonTraitStoreTests
    {
        private GameObject _go;
        private SummonTraitStore _store;
        private readonly List<ScriptableObject> _createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            ResetSceneSingletonInstance();
            ResetGlobalEventBus();

            _go = new GameObject("TestSummonTraitStore");
            _store = _go.AddComponent<SummonTraitStore>();

            // EditMode 테스트에서는 Unity 가 MonoBehaviour 라이프사이클(Awake/OnEnable) 을
            // 자동 발화하지 않으므로 reflection 으로 직접 호출하여 구독 상태를 갖춘다.
            InvokeNonPublic(_store, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                InvokeNonPublic(_store, "OnDisable"); // 명시적 해제 — EditMode 에서는 자동 발화 안 됨
                Object.DestroyImmediate(_go);
            }

            foreach (var asset in _createdAssets)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }
            _createdAssets.Clear();

            ResetSceneSingletonInstance();
            ResetGlobalEventBus();
        }

        /// <summary>EditMode 에서 자동 발화하지 않는 MonoBehaviour 비공개 라이프사이클 메서드를 reflection 으로 호출한다.</summary>
        private static void InvokeNonPublic(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(target, null);
        }

        /// <summary>테스트용 UnitLoadOutData 를 in-memory 로 생성하고 TearDown 자동 정리 대상에 등록한다.</summary>
        private UnitLoadOutData CreateUnit()
        {
            var so = ScriptableObject.CreateInstance<UnitLoadOutData>();
            _createdAssets.Add(so);
            return so;
        }

        /// <summary>테스트용 SynergyDefinitionData 를 in-memory 로 생성하고 TearDown 자동 정리 대상에 등록한다.</summary>
        private SynergyDefinitionData CreateTrait()
        {
            var so = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            _createdAssets.Add(so);
            return so;
        }

        /// <summary>SceneSingleton 의 정적 인스턴스 캐시를 비워 테스트 간 격리를 보장한다.</summary>
        private static void ResetSceneSingletonInstance()
        {
            var instanceField = typeof(SceneSingleton<SummonTraitStore>)
                .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }

        /// <summary>GlobalEventBus 의 정적 핸들러 사전을 비워 이전 테스트의 구독이 누수되지 않도록 보장한다.</summary>
        private static void ResetGlobalEventBus()
        {
            var handlersField = typeof(GlobalEventBus)
                .GetField("Handlers", BindingFlags.NonPublic | BindingFlags.Static);
            if (handlersField?.GetValue(null) is IDictionary handlers)
            {
                handlers.Clear();
            }
        }

        // ── DoD-S3: 런타임 보관 ──

        [Test]
        public void Test_TC001_Initialize_Then_GetTrait_Returns_Assigned_Value()
        {
            var unit = CreateUnit();
            var trait = CreateTrait();
            var map = new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } };

            _store.Initialize(map);

            Assert.That(_store.GetTrait(unit), Is.EqualTo(trait));
        }

        [Test]
        public void Test_TC002_Initialize_Multiple_Entries_All_Retrievable()
        {
            var unit1 = CreateUnit();
            var unit2 = CreateUnit();
            var unit3 = CreateUnit();
            var trait1 = CreateTrait();
            var trait2 = CreateTrait();
            var trait3 = CreateTrait();
            var map = new Dictionary<UnitLoadOutData, SynergyDefinitionData>
            {
                { unit1, trait1 }, { unit2, trait2 }, { unit3, trait3 }
            };

            _store.Initialize(map);

            Assert.That(_store.GetTrait(unit1), Is.EqualTo(trait1));
            Assert.That(_store.GetTrait(unit2), Is.EqualTo(trait2));
            Assert.That(_store.GetTrait(unit3), Is.EqualTo(trait3));
            Assert.That(_store.All.Count, Is.EqualTo(3));
        }

        [Test]
        public void Test_TC003_GetTrait_Unassigned_Key_Returns_Null()
        {
            var assigned = CreateUnit();
            var unassigned = CreateUnit();
            var trait = CreateTrait();
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { assigned, trait } });

            Assert.That(_store.GetTrait(unassigned), Is.Null);
        }

        [Test]
        public void Test_TC004_Initialize_ReCalled_Replaces_Existing_Data()
        {
            var firstUnit = CreateUnit();
            var secondUnit = CreateUnit();
            var firstTrait = CreateTrait();
            var secondTrait = CreateTrait();

            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { firstUnit, firstTrait } });
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { secondUnit, secondTrait } });

            Assert.That(_store.GetTrait(firstUnit), Is.Null, "이전 배정은 두 번째 Initialize 후 더 이상 유효하지 않아야 함");
            Assert.That(_store.GetTrait(secondUnit), Is.EqualTo(secondTrait));
            Assert.That(_store.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_TC005_Initialize_With_Empty_Map_Yields_Empty_Store()
        {
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData>());

            Assert.That(_store.All.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC006_Initialize_Null_Logs_Error_And_Preserves_State()
        {
            var unit = CreateUnit();
            var trait = CreateTrait();
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });

            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitStore\].*traitMap.*null"));
            _store.Initialize(null);

            Assert.That(_store.GetTrait(unit), Is.EqualTo(trait), "null 입력 시 기존 데이터가 보존되어야 함");
            Assert.That(_store.All.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_TC007_GetTrait_Null_Logs_Error_And_Returns_Null()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[SummonTraitStore\].*unit.*null"));
            var result = _store.GetTrait(null);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Test_TC008_Fresh_Instance_Is_Empty()
        {
            Assert.That(_store.All.Count, Is.EqualTo(0));
            Assert.That(_store.GetTrait(CreateUnit()), Is.Null);
        }

        // ── DoD-S4: 전장 종료 자동 초기화 ──

        [Test]
        public void Test_TC010_OnBattleWin_Auto_Clears_Store()
        {
            var unit = CreateUnit();
            var trait = CreateTrait();
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });

            GlobalEventBus.Publish(new OnBattleWinEventDto());

            Assert.That(_store.All.Count, Is.EqualTo(0));
            Assert.That(_store.GetTrait(unit), Is.Null);
        }

        [Test]
        public void Test_TC011_OnBattleLose_Auto_Clears_Store()
        {
            var unit = CreateUnit();
            var trait = CreateTrait();
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });

            GlobalEventBus.Publish(new OnBattleLoseEventDto());

            Assert.That(_store.All.Count, Is.EqualTo(0));
            Assert.That(_store.GetTrait(unit), Is.Null);
        }

        [Test]
        public void Test_TC012_Clear_Directly_Empties_Store()
        {
            var unit = CreateUnit();
            var trait = CreateTrait();
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });

            _store.Clear();

            Assert.That(_store.All.Count, Is.EqualTo(0));
            Assert.That(_store.GetTrait(unit), Is.Null);
        }

        [Test]
        public void Test_TC013_Clear_On_Empty_Store_NoThrow_NoChange()
        {
            Assert.DoesNotThrow(() => _store.Clear());
            Assert.That(_store.All.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_TC014_Disabled_Component_Does_Not_AutoClear_On_BattleEnd()
        {
            var unit = CreateUnit();
            var trait = CreateTrait();
            _store.Initialize(new Dictionary<UnitLoadOutData, SynergyDefinitionData> { { unit, trait } });

            InvokeNonPublic(_store, "OnDisable"); // 비활성화 시뮬레이션 — 구독 해제

            GlobalEventBus.Publish(new OnBattleWinEventDto());
            GlobalEventBus.Publish(new OnBattleLoseEventDto());

            Assert.That(_store.All.Count, Is.EqualTo(1), "구독 해제 후 이벤트는 자동 Clear 를 발동하지 않아야 함");
            Assert.That(_store.GetTrait(unit), Is.EqualTo(trait));
        }
    }
}
