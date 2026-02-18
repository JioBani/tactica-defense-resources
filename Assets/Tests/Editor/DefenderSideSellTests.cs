using Common.Scripts.Draggable;
using NUnit.Framework;
using Scenes.Battle.Feature.Sells;
using UnityEngine;

namespace Tests.Editor
{
    public class DefenderSideSellTests
    {
        private GameObject _waitingZoneGo;
        private GameObject _battleCellGo;
        private GameObject _draggableGo;

        private ExclusiveDropZone2D _waitingZone;
        private DefenderSideSell _battleCell;
        private Draggable2D _draggable;

        [SetUp]
        public void SetUp()
        {
            _waitingZoneGo = new GameObject("WaitingZone");
            _waitingZoneGo.AddComponent<BoxCollider2D>();
            _waitingZone = _waitingZoneGo.AddComponent<ExclusiveDropZone2D>();

            _battleCellGo = new GameObject("BattleCell");
            _battleCellGo.AddComponent<BoxCollider2D>();
            _battleCell = _battleCellGo.AddComponent<DefenderSideSell>();

            _draggableGo = new GameObject("Draggable");
            _draggable = _draggableGo.AddComponent<Draggable2D>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_waitingZoneGo);
            Object.DestroyImmediate(_battleCellGo);
            Object.DestroyImmediate(_draggableGo);
        }

        // ── WouldIncreaseBattleCount ──

        [Test]
        public void WouldIncrease_FromWaitingArea_ToEmptyCell_ReturnsTrue()
        {
            bool result = DefenderSideSell.WouldIncreaseBattleCount(_waitingZone, null);
            Assert.IsTrue(result);
        }

        [Test]
        public void WouldIncrease_FromWaitingArea_ToOccupiedCell_ReturnsFalse()
        {
            bool result = DefenderSideSell.WouldIncreaseBattleCount(_waitingZone, _draggable);
            Assert.IsFalse(result);
        }

        [Test]
        public void WouldIncrease_FromBattleArea_ToEmptyCell_ReturnsFalse()
        {
            bool result = DefenderSideSell.WouldIncreaseBattleCount(_battleCell, null);
            Assert.IsFalse(result);
        }

        [Test]
        public void WouldIncrease_FromBattleArea_ToOccupiedCell_ReturnsFalse()
        {
            bool result = DefenderSideSell.WouldIncreaseBattleCount(_battleCell, _draggable);
            Assert.IsFalse(result);
        }
    }
}
