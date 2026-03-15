using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using Common.Scripts.StatusEffect;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// SynergyStatusEffect의 생명주기와 ActiveTier 구독을 검증한다.
    /// 콜백 호출을 기록하는 RecordingSynergyStatusEffect로 간접 검증한다.
    /// </summary>
    public class SynergyStatusEffectTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            var tiers = new List<SynergyTier>
            {
                CreateTier(1, 2),
                CreateTier(2, 4),
            };
            SetTiers(_definition, tiers);
            _activation = new SynergyActivation(_definition);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        // ── OnApply 시 OnSynergyActivated가 현재 티어로 호출됨 ──

        [Test]
        public void OnApply_CallsOnSynergyActivated_WithCurrentTier()
        {
            _activation.Recalculate(2); // 1티어 활성화
            var effect = new RecordingSynergyStatusEffect();

            ApplyEffect(effect);

            Assert.AreEqual(1, effect.ActivatedTiers.Count);
            Assert.AreEqual(1, effect.ActivatedTiers[0].Tier);
        }

        // ── 티어 변경 시 OnSynergyTierChanged가 새 티어로 호출됨 ──

        [Test]
        public void TierChange_CallsOnSynergyTierChanged_WithNewTier()
        {
            _activation.Recalculate(2); // 1티어
            var effect = new RecordingSynergyStatusEffect();
            ApplyEffect(effect);

            _activation.Recalculate(4); // 1티어 → 2티어

            Assert.AreEqual(1, effect.TierChangedTiers.Count);
            Assert.AreEqual(2, effect.TierChangedTiers[0].Tier);
        }

        // ── 비활성화 시 OnSynergyDeactivated 호출 + IsExpired ──

        [Test]
        public void Deactivation_CallsOnSynergyDeactivated_AndRequestsRemove()
        {
            _activation.Recalculate(2); // 1티어
            var effect = new RecordingSynergyStatusEffect();
            ApplyEffect(effect);

            _activation.Recalculate(0); // 비활성화

            Assert.AreEqual(1, effect.DeactivatedCount);
            Assert.IsTrue(effect.IsExpired);
        }

        // ── OnRemove 후 ActiveTier 변경해도 콜백 미호출 ──

        [Test]
        public void AfterRemove_TierChange_DoesNotCallCallbacks()
        {
            _activation.Recalculate(2); // 1티어
            var effect = new RecordingSynergyStatusEffect();
            ApplyEffect(effect);

            effect.OnRemove(); // 구독 해제

            _activation.Recalculate(4); // 티어 변경

            Assert.AreEqual(0, effect.TierChangedTiers.Count);
        }

        // ── 비활성 상태에서 Apply 시 OnSynergyActivated 미호출 ──

        [Test]
        public void OnApply_WhenInactive_DoesNotCallOnSynergyActivated()
        {
            // 시너지 비활성 상태 (Recalculate 안 함, 카운트 0)
            var effect = new RecordingSynergyStatusEffect();
            ApplyEffect(effect);

            Assert.AreEqual(0, effect.ActivatedTiers.Count);
        }

        /// <summary>SSE를 SynergyStatusEffectContext와 함께 Apply한다.</summary>
        private void ApplyEffect(SynergyStatusEffect<SynergyStatusEffectContext> effect)
        {
            var context = new SynergyStatusEffectContext(_activation, _definition, null);
            effect.OnApply(context);
        }

        /// <summary>리플렉션으로 tier와 requiredCount가 설정된 SynergyTier를 생성한다.</summary>
        private static SynergyTier CreateTier(int tierLevel, int requiredCount)
        {
            var tier = new SynergyTier();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            object boxed = tier;
            typeof(SynergyTier).GetField("tier", flags).SetValue(boxed, tierLevel);
            typeof(SynergyTier).GetField("requiredCount", flags).SetValue(boxed, requiredCount);
            return (SynergyTier)boxed;
        }

        /// <summary>리플렉션으로 SynergyDefinitionData의 tiers 필드를 설정한다.</summary>
        private static void SetTiers(SynergyDefinitionData definition, List<SynergyTier> tiers)
        {
            var tiersField = typeof(SynergyDefinitionData)
                .GetField("tiers", BindingFlags.NonPublic | BindingFlags.Instance);
            tiersField.SetValue(definition, tiers);
        }

        /// <summary>콜백 호출을 기록하는 테스트용 SynergyStatusEffect 구현체.</summary>
        private class RecordingSynergyStatusEffect : SynergyStatusEffect<SynergyStatusEffectContext>
        {
            public RecordingSynergyStatusEffect() : base(null) { }

            public readonly List<SynergyTier> ActivatedTiers = new();
            public readonly List<SynergyTier> TierChangedTiers = new();
            public int DeactivatedCount;

            protected override void OnSynergyActivated(SynergyTier tier)
            {
                ActivatedTiers.Add(tier);
            }

            protected override void OnSynergyTierChanged(SynergyTier newTier)
            {
                TierChangedTiers.Add(newTier);
            }

            protected override void OnSynergyDeactivated()
            {
                DeactivatedCount++;
            }
        }
    }
}
