using Common.Data.Configs;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class StarProbabilityConfigTests
    {
        private StarProbabilityConfig CreateConfig(params StarProbabilityEntry[] entries)
        {
            var config = ScriptableObject.CreateInstance<StarProbabilityConfig>();
            config.entries = entries;
            return config;
        }

        // entries가 비어있으면 항상 1성을 반환해야 한다
        [Test]
        public void PickStar_NoEntries_ReturnsOneStar()
        {
            var config = CreateConfig();

            int star = config.PickStar(5);

            Assert.AreEqual(1, star);
        }

        // 현재 레벨이 모든 entry의 level보다 낮으면 1성을 반환해야 한다
        [Test]
        public void PickStar_LevelBelowAllEntries_ReturnsOneStar()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 4, twoStarRate = 1f, threeStarRate = 0f }
            );

            int star = config.PickStar(3);

            Assert.AreEqual(1, star);
        }

        // threeStarRate=1이면 항상 3성을 반환해야 한다
        [Test]
        public void PickStar_ThreeStarRate100Percent_ReturnsThreeStar()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 1, twoStarRate = 0f, threeStarRate = 1f }
            );

            // Random.value는 [0, 1) 범위이므로 threeStarRate=1이면 항상 3성
            int star = config.PickStar(1);

            Assert.AreEqual(3, star);
        }

        // twoStarRate=1, threeStarRate=0이면 항상 2성을 반환해야 한다
        [Test]
        public void PickStar_TwoStarRate100Percent_ReturnsTwoStar()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 1, twoStarRate = 1f, threeStarRate = 0f }
            );

            // threeStarRate=0이므로 roll < 0 불가, roll < 0+1=1이므로 항상 2성
            int star = config.PickStar(1);

            Assert.AreEqual(2, star);
        }

        // 2성/3성 확률이 모두 0이면 항상 1성을 반환해야 한다
        [Test]
        public void PickStar_ZeroRates_ReturnsOneStar()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 1, twoStarRate = 0f, threeStarRate = 0f }
            );

            int star = config.PickStar(5);

            Assert.AreEqual(1, star);
        }

        // 현재 레벨 이하에서 가장 가까운 entry를 사용해야 한다
        [Test]
        public void PickStar_MatchesClosestLowerLevel()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 1, twoStarRate = 0f, threeStarRate = 0f },
                new StarProbabilityEntry { level = 4, twoStarRate = 1f, threeStarRate = 0f },
                new StarProbabilityEntry { level = 7, twoStarRate = 0f, threeStarRate = 1f }
            );

            // 레벨 5는 level=4 entry에 매칭 → twoStarRate=1 → 항상 2성
            int star = config.PickStar(5);
            Assert.AreEqual(2, star);

            // 레벨 8은 level=7 entry에 매칭 → threeStarRate=1 → 항상 3성
            star = config.PickStar(8);
            Assert.AreEqual(3, star);
        }

        // 정확히 일치하는 레벨의 entry를 사용해야 한다
        [Test]
        public void PickStar_ExactLevelMatch()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 4, twoStarRate = 1f, threeStarRate = 0f }
            );

            // 정확히 level=4에 매칭
            int star = config.PickStar(4);

            Assert.AreEqual(2, star);
        }

        // 확률이 섞여있어도 반환값은 항상 1~3 범위여야 한다
        [Test]
        public void PickStar_ReturnsValidStarRange()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 1, twoStarRate = 0.3f, threeStarRate = 0.1f }
            );

            for (int i = 0; i < 100; i++)
            {
                int star = config.PickStar(5);
                Assert.That(star, Is.InRange(1, 3), $"성급이 1~3 범위를 벗어남: {star}");
            }
        }

        // entries가 비어있으면 FindEntry는 null을 반환해야 한다
        [Test]
        public void FindEntry_NoEntries_ReturnsNull()
        {
            var config = CreateConfig();

            StarProbabilityEntry? result = config.FindEntry(5);

            Assert.IsFalse(result.HasValue);
        }

        // 현재 레벨 이하에서 가장 가까운 entry를 반환해야 한다
        [Test]
        public void FindEntry_ReturnsClosestLowerOrEqualEntry()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 1, twoStarRate = 0.1f, threeStarRate = 0.01f },
                new StarProbabilityEntry { level = 4, twoStarRate = 0.2f, threeStarRate = 0.05f },
                new StarProbabilityEntry { level = 7, twoStarRate = 0.3f, threeStarRate = 0.1f }
            );

            // 레벨 5는 level=4 entry에 매칭
            var entry = config.FindEntry(5);
            Assert.IsTrue(entry.HasValue);
            Assert.AreEqual(4, entry.Value.level);
            Assert.AreEqual(0.2f, entry.Value.twoStarRate);
            Assert.AreEqual(0.05f, entry.Value.threeStarRate);
        }

        // 레벨이 모든 entry보다 낮으면 null을 반환해야 한다
        [Test]
        public void FindEntry_LevelBelowAll_ReturnsNull()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 4, twoStarRate = 0.2f, threeStarRate = 0.05f }
            );

            StarProbabilityEntry? result = config.FindEntry(3);

            Assert.IsFalse(result.HasValue);
        }

        // 정확히 일치하는 레벨의 entry를 반환해야 한다
        [Test]
        public void FindEntry_ExactMatch_ReturnsEntry()
        {
            var config = CreateConfig(
                new StarProbabilityEntry { level = 4, twoStarRate = 0.25f, threeStarRate = 0.05f }
            );

            var entry = config.FindEntry(4);

            Assert.IsTrue(entry.HasValue);
            Assert.AreEqual(0.25f, entry.Value.twoStarRate);
            Assert.AreEqual(0.05f, entry.Value.threeStarRate);
        }

        [TearDown]
        public void TearDown()
        {
            // ScriptableObject.CreateInstance로 생성한 인스턴스 정리는 GC가 처리
        }
    }
}
