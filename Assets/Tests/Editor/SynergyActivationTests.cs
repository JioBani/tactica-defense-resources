using System.Collections.Generic;
using System.Reflection;
using Common.Data.Synergies;
using NUnit.Framework;
using Scenes.Battle.Feature.Synergy;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// SynergyActivation.FindActiveTier 판정 로직을 검증한다.
    /// Recalculate(int) 호출 → ActiveTier 결과로 간접 검증한다.
    /// </summary>
    public class SynergyActivationTests
    {
        private SynergyDefinitionData _definition;
        private SynergyActivation _activation;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<SynergyDefinitionData>();

            // 리플렉션으로 tiers 설정: 1티어(requiredCount 2), 2티어(requiredCount 4)
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

        // ── 카운트 0 → 티어 없음 ──

        [Test]
        public void FindActiveTier_CountZero_ReturnsNull()
        {
            _activation.Recalculate(0);

            Assert.AreEqual(0, _activation.Count);
            Assert.IsNull(_activation.ActiveTier);
        }

        // ── 임계치 미달 → 티어 없음 ──

        [Test]
        public void FindActiveTier_BelowFirstThreshold_ReturnsNull()
        {
            _activation.Recalculate(1);

            Assert.AreEqual(1, _activation.Count);
            Assert.IsNull(_activation.ActiveTier);
        }

        // ── 첫 번째 임계치 충족 → 1티어 활성 ──

        [Test]
        public void FindActiveTier_MeetsFirstThreshold_ReturnsFirstTier()
        {
            _activation.Recalculate(2);

            Assert.AreEqual(2, _activation.Count);
            Assert.IsNotNull(_activation.ActiveTier);
            Assert.AreEqual(1, _activation.ActiveTier.Value.Tier);
            Assert.AreEqual(2, _activation.ActiveTier.Value.RequiredCount);
        }

        // ── 상위 임계치 충족 → 상위 티어 활성 ──

        [Test]
        public void FindActiveTier_MeetsSecondThreshold_ReturnsSecondTier()
        {
            _activation.Recalculate(4);

            Assert.IsNotNull(_activation.ActiveTier);
            Assert.AreEqual(2, _activation.ActiveTier.Value.Tier);
            Assert.AreEqual(4, _activation.ActiveTier.Value.RequiredCount);
        }

        // ── 카운트 감소 → 하위 티어로 복귀 ──

        [Test]
        public void FindActiveTier_CountDecreases_RevertsToLowerTier()
        {
            _activation.Recalculate(4);
            Assert.AreEqual(2, _activation.ActiveTier.Value.Tier);

            _activation.Recalculate(2);
            Assert.AreEqual(1, _activation.ActiveTier.Value.Tier);
        }

        // ── 카운트 감소로 모든 임계치 미달 → 티어 없음 ──

        [Test]
        public void FindActiveTier_CountDecreasesToZero_ReturnsNull()
        {
            _activation.Recalculate(4);
            Assert.IsNotNull(_activation.ActiveTier);

            _activation.Recalculate(0);
            Assert.IsNull(_activation.ActiveTier);
        }

        // ── tiers가 null인 경우 → 티어 없음 ──

        [Test]
        public void FindActiveTier_TiersNull_ReturnsNull()
        {
            var emptyDefinition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            // tiers 필드를 설정하지 않으면 null
            var state = new SynergyActivation(emptyDefinition);

            state.Recalculate(10);

            Assert.IsNull(state.ActiveTier);

            Object.DestroyImmediate(emptyDefinition);
        }

        // ── tiers가 비어있는 경우 → 티어 없음 ──

        [Test]
        public void FindActiveTier_TiersEmpty_ReturnsNull()
        {
            var emptyDefinition = ScriptableObject.CreateInstance<SynergyDefinitionData>();
            SetTiers(emptyDefinition, new List<SynergyTier>());
            var state = new SynergyActivation(emptyDefinition);

            state.Recalculate(10);

            Assert.IsNull(state.ActiveTier);

            Object.DestroyImmediate(emptyDefinition);
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
    }
}
