using System.Collections.Generic;
using NUnit.Framework;
using Scenes.Battle.Feature.Fusion;

namespace Tests.Editor
{
    public class FusionGroupDetectorTests
    {
        private FusionGroupDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _detector = new FusionGroupDetector();
        }

        // ── 기본 합성 탐지 ──

        [Test]
        public void FindFusionGroup_ThreeSameUnits_ReturnsGroupIndices()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 1),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 2),
            };

            var result = _detector.FindFusionGroup(candidates);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Length);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, result);
        }

        [Test]
        public void FindFusionGroup_TwoSameUnits_ReturnsNull()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 1),
            };

            var result = _detector.FindFusionGroup(candidates);

            Assert.IsNull(result);
        }

        [Test]
        public void FindFusionGroup_EmptyList_ReturnsNull()
        {
            var result = _detector.FindFusionGroup(new List<FusionCandidate>());

            Assert.IsNull(result);
        }

        [Test]
        public void FindFusionGroup_NullList_ReturnsNull()
        {
            var result = _detector.FindFusionGroup(null);

            Assert.IsNull(result);
        }

        // ── 다른 종류/성급 혼합 ──

        [Test]
        public void FindFusionGroup_DifferentUnitTypes_NoFusion()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 2, star: 1, index: 1),
                new FusionCandidate(unitDefinitionId: 3, star: 1, index: 2),
            };

            var result = _detector.FindFusionGroup(candidates);

            Assert.IsNull(result, "서로 다른 유닛 종류는 합성되지 않아야 한다");
        }

        [Test]
        public void FindFusionGroup_SameUnitDifferentStars_NoFusion()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 2, index: 1),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 2),
            };

            var result = _detector.FindFusionGroup(candidates);

            Assert.IsNull(result, "같은 유닛이라도 성급이 다르면 합성되지 않아야 한다");
        }

        [Test]
        public void FindFusionGroup_MixedUnits_FindsCorrectGroup()
        {
            // 유닛A 2개 + 유닛B 3개 → 유닛B 그룹만 합성
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 2, star: 1, index: 1),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 2),
                new FusionCandidate(unitDefinitionId: 2, star: 1, index: 3),
                new FusionCandidate(unitDefinitionId: 2, star: 1, index: 4),
            };

            var result = _detector.FindFusionGroup(candidates);

            Assert.IsNotNull(result);
            // 유닛B(id=2) 그룹의 인덱스만 포함
            CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, result);
        }

        // ── 연쇄 합성 시나리오 ──

        [Test]
        public void FindFusionGroup_SixSameUnits_ReturnsAllGroupIndices()
        {
            // 6개 동일 유닛 → 전체 그룹 인덱스를 반환 (연쇄는 Service 레벨에서 처리)
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 1),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 2),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 3),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 4),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 5),
            };

            var result = _detector.FindFusionGroup(candidates);

            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Length);
        }

        // ── maxStar 제한 ──

        [Test]
        public void FindFusionGroup_ThreeStar3Units_WithMaxStar3_ReturnsNull()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 1),
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 2),
            };

            var result = _detector.FindFusionGroup(candidates, maxStar: 3);

            Assert.IsNull(result, "maxStar=3이면 3성 그룹은 합성되지 않아야 한다");
        }

        // ── HasFusionGroup ──

        [Test]
        public void HasFusionGroup_ThreeSameUnits_ReturnsTrue()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 1),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 2),
            };

            Assert.IsTrue(_detector.HasFusionGroup(candidates));
        }

        [Test]
        public void HasFusionGroup_TwoSameUnits_ReturnsFalse()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 1),
            };

            Assert.IsFalse(_detector.HasFusionGroup(candidates));
        }
    }
}
