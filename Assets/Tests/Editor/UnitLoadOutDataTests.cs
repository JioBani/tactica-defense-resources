using System.Reflection;
using Common.Data.Units.UnitLoadOuts;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class UnitLoadOutDataTests
    {
        private UnitLoadOutData _loadOut;

        [SetUp]
        public void SetUp()
        {
            _loadOut = ScriptableObject.CreateInstance<UnitLoadOutData>();
            // costByStar 기본값: { 0, 1, 3, 9 }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loadOut);
        }

        [Test]
        public void GetCostByStar_1성_기본비용_반환()
        {
            Assert.AreEqual(1, _loadOut.GetCostByStar(1));
        }

        [Test]
        public void GetCostByStar_2성_비용_반환()
        {
            Assert.AreEqual(3, _loadOut.GetCostByStar(2));
        }

        [Test]
        public void GetCostByStar_3성_비용_반환()
        {
            Assert.AreEqual(9, _loadOut.GetCostByStar(3));
        }

        [Test]
        public void GetCostByStar_범위초과_1성비용_반환()
        {
            Assert.AreEqual(1, _loadOut.GetCostByStar(99));
        }

        [Test]
        public void GetCostByStar_음수_1성비용_반환()
        {
            Assert.AreEqual(1, _loadOut.GetCostByStar(-1));
        }

        [Test]
        public void GetCostByStar_커스텀비용_적용()
        {
            var field = typeof(UnitLoadOutData).GetField("costByStar", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(_loadOut, new int[] { 0, 2, 6, 18 });

            Assert.AreEqual(2, _loadOut.GetCostByStar(1));
            Assert.AreEqual(6, _loadOut.GetCostByStar(2));
            Assert.AreEqual(18, _loadOut.GetCostByStar(3));
        }

        // ── GetRefundMana 테스트 ──

        [Test]
        public void GetRefundMana_1성_환원비용_반환()
        {
            Assert.AreEqual(1, _loadOut.GetRefundMana(1));
        }

        [Test]
        public void GetRefundMana_2성_환원비용_반환()
        {
            Assert.AreEqual(2, _loadOut.GetRefundMana(2));
        }

        [Test]
        public void GetRefundMana_3성_환원비용_반환()
        {
            Assert.AreEqual(8, _loadOut.GetRefundMana(3));
        }

        [Test]
        public void GetRefundMana_3성_강화포함_환원비용_반환()
        {
            Assert.AreEqual(10, _loadOut.GetRefundMana(3, 2));
        }

        [Test]
        public void GetRefundMana_범위초과_1성환원비용_반환()
        {
            Assert.AreEqual(1, _loadOut.GetRefundMana(99));
        }
    }
}
