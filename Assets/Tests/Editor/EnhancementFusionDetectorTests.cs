using System.Collections.Generic;
using NUnit.Framework;
using Scenes.Battle.Feature.Fusion;

namespace Tests.Editor
{
    public class EnhancementFusionDetectorTests
    {
        private EnhancementFusionDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _detector = new EnhancementFusionDetector();
        }

        // ── 강화 합성 탐지 ──

        [Test]
        public void FindEnhancementPair_Star3AndSameTypeStar2_ReturnsPair()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 2, index: 1),
            };

            var result = _detector.FindEnhancementPair(candidates);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Value.TargetIndex);
            Assert.AreEqual(1, result.Value.MaterialIndex);
        }

        [Test]
        public void FindEnhancementPair_Star3AndDifferentTypeStar2_ReturnsNull()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 0),
                new FusionCandidate(unitDefinitionId: 2, star: 2, index: 1),
            };

            var result = _detector.FindEnhancementPair(candidates);

            Assert.IsNull(result, "다른 종류의 유닛은 강화 합성되지 않아야 한다");
        }

        [Test]
        public void FindEnhancementPair_Star3AndStar1_ReturnsNull()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 1, index: 1),
            };

            var result = _detector.FindEnhancementPair(candidates);

            Assert.IsNull(result, "재료가 2성이 아니면 강화 합성되지 않아야 한다");
        }

        [Test]
        public void FindEnhancementPair_BothStar2_ReturnsNull()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 2, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 2, index: 1),
            };

            var result = _detector.FindEnhancementPair(candidates);

            Assert.IsNull(result, "타겟이 3성 미만이면 강화 합성되지 않아야 한다");
        }

        [Test]
        public void FindEnhancementPair_EmptyList_ReturnsNull()
        {
            var result = _detector.FindEnhancementPair(new List<FusionCandidate>());

            Assert.IsNull(result);
        }

        [Test]
        public void FindEnhancementPair_NullList_ReturnsNull()
        {
            var result = _detector.FindEnhancementPair(null);

            Assert.IsNull(result);
        }

        // ── HasEnhancementPair ──

        [Test]
        public void HasEnhancementPair_ValidPair_ReturnsTrue()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 0),
                new FusionCandidate(unitDefinitionId: 1, star: 2, index: 1),
            };

            Assert.IsTrue(_detector.HasEnhancementPair(candidates));
        }

        [Test]
        public void HasEnhancementPair_NoPair_ReturnsFalse()
        {
            var candidates = new List<FusionCandidate>
            {
                new FusionCandidate(unitDefinitionId: 1, star: 3, index: 0),
            };

            Assert.IsFalse(_detector.HasEnhancementPair(candidates));
        }
    }
}
