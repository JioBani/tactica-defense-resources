using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.SerializableDictionary;
using Common.Scripts.StatusEffect;
using Common.Scripts.StatusEffect.HookProvider;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyEffects;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.Attackables;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// FreljordSynergyStatusEffect(프렐요드)의 둔화 적용/갱신/해제를 검증한다.
    /// IOnAttackHitHook.OnAttackHit을 직접 호출하여 HookProvider 없이 SSE 로직만 테스트한다.
    /// </summary>
    public class FreljordSynergyStatusEffectTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;

        private Unit _attackerUnit;
        private GameObject _attackerGo;

        private Unit _victimUnit;
        private Victim _victim;
        private StatusEffectController _victimStatusEffectController;
        private GameObject _victimGo;

        private const float Tier1SlowPercent = 0.2f;
        private const float Tier2SlowPercent = 0.35f;
        private const float SlowDuration = 3f;
        private const float BaseMoveSpeed = 5f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();

            var constants1 = new SerializableDictionary<string, float>();
            constants1["slowPercent"] = Tier1SlowPercent;
            constants1["slowDuration"] = SlowDuration;
            var constants2 = new SerializableDictionary<string, float>();
            constants2["slowPercent"] = Tier2SlowPercent;
            constants2["slowDuration"] = SlowDuration;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
                CreateTier(2, 4, constants2),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            // 공격자 유닛 (SSE 부여 대상)
            _attackerGo = new GameObject("AttackerUnit");
            _attackerUnit = _attackerGo.AddComponent<StubUnit>();

            // 피해자 유닛 (둔화 적용 대상)
            _victimGo = new GameObject("VictimUnit");
            _victimUnit = _victimGo.AddComponent<StubUnit>();
            _victimUnit.StatSheet.MoveSpeed.SetBaseValue(BaseMoveSpeed);

            _victimStatusEffectController = _victimGo.AddComponent<StatusEffectController>();
            SetPrivateField(_victimUnit, "statusEffectController", _victimStatusEffectController);

            _victim = _victimGo.AddComponent<Victim>();
            SetPrivateField(_victim, "unit", _victimUnit);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_attackerGo);
            Object.DestroyImmediate(_victimGo);
            Object.DestroyImmediate(_definition);
        }

        // ── 적중 시 victim의 MoveSpeed에 둔화 수정자 적용 ──

        [Test]
        public void OnAttackHit_AppliesSlowToVictimMoveSpeed()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            effect.OnAttackHit(new AttackContext(0, null, _victim));

            float expected = BaseMoveSpeed * (1f - Tier1SlowPercent);
            Assert.AreEqual(expected, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── 동일 대상 재적중 시 리프레시 (수정자 중복 없음) ──

        [Test]
        public void OnAttackHit_SameVictimTwice_DoesNotStackSlow()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            effect.OnAttackHit(new AttackContext(0, null, _victim));
            effect.OnAttackHit(new AttackContext(0, null, _victim));

            float expected = BaseMoveSpeed * (1f - Tier1SlowPercent);
            Assert.AreEqual(expected, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── 비활성화 시 모든 둔화 제거 ──

        [Test]
        public void OnSynergyDeactivated_RemovesAllSlows()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            effect.OnAttackHit(new AttackContext(0, null, _victim));

            _activation.Recalculate(0);

            Assert.AreEqual(BaseMoveSpeed, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── 티어 변경 후 활성 둔화의 수치가 갱신됨 ──

        [Test]
        public void OnSynergyTierChanged_UpdatesActiveSlowPercent()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            effect.OnAttackHit(new AttackContext(0, null, _victim));

            _activation.Recalculate(4);

            float expected = BaseMoveSpeed * (1f - Tier2SlowPercent);
            Assert.AreEqual(expected, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── 티어 변경 후 새 적중 시 새 수치 적용 ──

        [Test]
        public void OnSynergyTierChanged_NewHitUsesNewPercent()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            _activation.Recalculate(4);

            // 티어 변경 후 첫 적중
            effect.OnAttackHit(new AttackContext(0, null, _victim));

            float expected = BaseMoveSpeed * (1f - Tier2SlowPercent);
            Assert.AreEqual(expected, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── OnRemove 시 모든 둔화 제거 (안전장치) ──

        [Test]
        public void OnRemove_RemovesAllSlows()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            effect.OnAttackHit(new AttackContext(0, null, _victim));

            effect.OnRemove();

            Assert.AreEqual(BaseMoveSpeed, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── 비활성 상태에서 Apply 시 적중해도 둔화 미적용 ──

        [Test]
        public void OnAttackHit_WhenInactive_DoesNotApplySlow()
        {
            var effect = CreateAndApplyEffect();

            effect.OnAttackHit(new AttackContext(0, null, _victim));

            Assert.AreEqual(BaseMoveSpeed, _victimUnit.StatSheet.MoveSpeed.CurrentValue, 0.01f);
        }

        // ── 헬퍼 ──

        /// <summary>FreljordSSE를 생성하고 Context와 함께 Apply한다.</summary>
        private FreljordSynergyStatusEffect CreateAndApplyEffect()
        {
            var effect = new FreljordSynergyStatusEffect(null);
            var context = new SynergyStatusEffectContext(_activation, _definition, _attackerUnit);
            effect.OnApply(context);
            return effect;
        }

        /// <summary>테스트용 Unit stub. Awake 호출 없이 StatSheet만 사용한다.</summary>
        private class StubUnit : Unit { }

        /// <summary>리플렉션으로 private 필드를 설정한다. 부모 클래스 필드도 탐색한다.</summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
        }

        /// <summary>리플렉션으로 SynergyTier를 생성한다.</summary>
        private static SynergyTier CreateTier(int tierLevel, int requiredCount,
            SerializableDictionary<string, float> constants)
        {
            var tier = new SynergyTier();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            object boxed = tier;
            typeof(SynergyTier).GetField("tier", flags).SetValue(boxed, tierLevel);
            typeof(SynergyTier).GetField("requiredCount", flags).SetValue(boxed, requiredCount);
            typeof(SynergyTier).GetField("constants", flags).SetValue(boxed, constants);
            return (SynergyTier)boxed;
        }

        /// <summary>리플렉션으로 SynergyDefinitionData의 tiers 필드를 설정한다.</summary>
        private static void SetTiers(SynergyDefinitionData definition, List<SynergyTier> tiers)
        {
            var tiersField = typeof(SynergyDefinitionData)
                .GetField("tiers", BindingFlags.NonPublic | BindingFlags.Instance);
            tiersField.SetValue(definition, tiers);
        }
    }
}
