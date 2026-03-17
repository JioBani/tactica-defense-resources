using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.SerializableDictionary;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using Scenes.Battle.Feature.Synergy.SynergyEffects;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.Attackables;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// GunslingerSynergyStatusEffect(총잡이)의 공격력 % 버프 적용/갱신/해제 및
    /// 4회 공격 추가 피해를 검증한다.
    /// </summary>
    public class GunslingerSynergyStatusEffectTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;
        private Unit _unit;
        private GameObject _gameObject;

        private const float Tier1AttackPercent = 0.22f;
        private const float Tier1ExtraDamage = 100f;
        private const float Tier2AttackPercent = 0.40f;
        private const float Tier2ExtraDamage = 200f;
        private const float BasePhysicalAttack = 50f;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();

            var constants1 = new SerializableDictionary<string, float>();
            constants1["attackPercent"] = Tier1AttackPercent;
            constants1["extraDamage"] = Tier1ExtraDamage;
            var constants2 = new SerializableDictionary<string, float>();
            constants2["attackPercent"] = Tier2AttackPercent;
            constants2["extraDamage"] = Tier2ExtraDamage;

            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2, constants1),
                CreateTier(2, 4, constants2),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);

            _gameObject = new GameObject("TestUnit");
            _unit = _gameObject.AddComponent<StubUnit>();
            _unit.StatSheet.PhysicalAttack.SetBaseValue(BasePhysicalAttack);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            Object.DestroyImmediate(_definition);
        }

        // ── 공격력 % 버프 ──

        [Test]
        public void OnSynergyActivated_AppliesAttackPercentModifier()
        {
            _activation.Recalculate(2);
            CreateAndApplyEffect();

            float expected = BasePhysicalAttack * (1f + Tier1AttackPercent);
            Assert.AreEqual(expected, _unit.StatSheet.PhysicalAttack.CurrentValue, 0.1f);
        }

        [Test]
        public void OnSynergyTierChanged_UpdatesAttackPercentModifier()
        {
            _activation.Recalculate(2);
            CreateAndApplyEffect();

            _activation.Recalculate(4);

            float expected = BasePhysicalAttack * (1f + Tier2AttackPercent);
            Assert.AreEqual(expected, _unit.StatSheet.PhysicalAttack.CurrentValue, 0.1f);
        }

        [Test]
        public void OnSynergyDeactivated_RemovesModifier()
        {
            _activation.Recalculate(2);
            CreateAndApplyEffect();

            _activation.Recalculate(0);

            Assert.AreEqual(BasePhysicalAttack, _unit.StatSheet.PhysicalAttack.CurrentValue, 0.1f);
        }

        [Test]
        public void OnRemove_RemovesModifier()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();

            effect.OnRemove();

            Assert.AreEqual(BasePhysicalAttack, _unit.StatSheet.PhysicalAttack.CurrentValue, 0.1f);
        }

        // ── 4회 공격 추가 피해 ──

        [Test]
        public void OnAttackHit_Before4thHit_NoDamage()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            var victim = CreateVictim(100f);

            for (int i = 0; i < 3; i++)
                effect.OnAttackHit(new AttackContext(0, null, victim));

            Assert.AreEqual(100f, victim.Unit.StatSheet.Health.Value, 0.1f);
        }

        [Test]
        public void OnAttackHit_On4thHit_DealsExtraDamage()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            var victim = CreateVictim(500f);

            for (int i = 0; i < 4; i++)
                effect.OnAttackHit(new AttackContext(0, null, victim));

            Assert.AreEqual(500f - Tier1ExtraDamage, victim.Unit.StatSheet.Health.Value, 0.1f);
        }

        [Test]
        public void OnAttackHit_On8thHit_DealsExtraDamageAgain()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            var victim = CreateVictim(500f);

            for (int i = 0; i < 8; i++)
                effect.OnAttackHit(new AttackContext(0, null, victim));

            Assert.AreEqual(500f - Tier1ExtraDamage * 2, victim.Unit.StatSheet.Health.Value, 0.1f);
        }

        [Test]
        public void OnSynergyDeactivated_ResetsAttackCount()
        {
            _activation.Recalculate(2);
            var effect = CreateAndApplyEffect();
            var victim = CreateVictim(500f);

            // 3회 공격 후 비활성화
            for (int i = 0; i < 3; i++)
                effect.OnAttackHit(new AttackContext(0, null, victim));
            _activation.Recalculate(0);

            // 재활성화 후 4회 공격해야 추가 피해
            _activation.Recalculate(2);
            for (int i = 0; i < 3; i++)
                effect.OnAttackHit(new AttackContext(0, null, victim));

            Assert.AreEqual(500f, victim.Unit.StatSheet.Health.Value, 0.1f,
                "비활성화로 카운트 리셋 → 3회 공격에는 추가 피해 없음");
        }

        // ── 헬퍼 ──

        /// <summary>GunslingerSSE를 생성하고 적용한다.</summary>
        private GunslingerSynergyStatusEffect CreateAndApplyEffect()
        {
            var effect = new GunslingerSynergyStatusEffect(null);
            var context = new SynergyStatusEffectContext(_activation, _definition, _unit);
            effect.OnApply(context);
            return effect;
        }

        /// <summary>테스트용 Victim을 생성한다.</summary>
        private Victim CreateVictim(float maxHealth)
        {
            var victimGo = new GameObject("Victim");
            var victimUnit = victimGo.AddComponent<StubUnit>();
            victimUnit.StatSheet.MaxHealth.SetBaseValue(maxHealth);
            victimUnit.StatSheet.SetCurrentHealth(maxHealth);
            var victim = victimGo.AddComponent<Victim>();
            SetVictimUnit(victim, victimUnit);
            return victim;
        }

        /// <summary>리플렉션으로 Victim.unit 필드를 설정한다.</summary>
        private static void SetVictimUnit(Victim victim, Unit unit)
        {
            var field = typeof(Victim).GetField("unit", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(victim, unit);
        }

        /// <summary>테스트용 Unit stub.</summary>
        private class StubUnit : Unit
        {
            protected override void Awake() { }
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
