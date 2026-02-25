using NUnit.Framework;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Units.ActionStates;

namespace Tests.Editor
{
    public class ActionStateTransitionServiceTests
    {
        private ActionStateTransitionService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new ActionStateTransitionService();
        }

        // ── CheckTransition: Downed 전환 ──

        [Test]
        public void CheckTransition_Idle_HealthZero_ReturnsDowned()
        {
            var result = _service.CheckTransition(
                ActionStateType.Idle, health: 0f, hasVictim: false, canMove: false);

            Assert.AreEqual(ActionStateType.Downed, result);
        }

        [Test]
        public void CheckTransition_Move_HealthZero_ReturnsDowned()
        {
            var result = _service.CheckTransition(
                ActionStateType.Move, health: 0f, hasVictim: false, canMove: true);

            Assert.AreEqual(ActionStateType.Downed, result);
        }

        [Test]
        public void CheckTransition_Attack_HealthZero_ReturnsDowned()
        {
            var result = _service.CheckTransition(
                ActionStateType.Attack, health: 0f, hasVictim: true, canMove: false);

            Assert.AreEqual(ActionStateType.Downed, result);
        }

        [Test]
        public void CheckTransition_Downed_HealthZero_StaysDowned()
        {
            var result = _service.CheckTransition(
                ActionStateType.Downed, health: 0f, hasVictim: false, canMove: false);

            Assert.AreEqual(ActionStateType.Downed, result);
        }

        [Test]
        public void CheckTransition_Freeze_HealthZero_StaysFreeze()
        {
            var result = _service.CheckTransition(
                ActionStateType.Freeze, health: 0f, hasVictim: false, canMove: false);

            Assert.AreEqual(ActionStateType.Freeze, result);
        }

        [Test]
        public void CheckTransition_Waiting_HealthZero_StaysWaiting()
        {
            var result = _service.CheckTransition(
                ActionStateType.Waiting, health: 0f, hasVictim: false, canMove: false);

            Assert.AreEqual(ActionStateType.Waiting, result);
        }

        // ── CheckTransition: 전투 상태 전환 ──

        [Test]
        public void CheckTransition_Idle_HasVictim_ReturnsAttack()
        {
            var result = _service.CheckTransition(
                ActionStateType.Idle, health: 100f, hasVictim: true, canMove: false);

            Assert.AreEqual(ActionStateType.Attack, result);
        }

        [Test]
        public void CheckTransition_Move_HasVictim_ReturnsAttack()
        {
            var result = _service.CheckTransition(
                ActionStateType.Move, health: 100f, hasVictim: true, canMove: true);

            Assert.AreEqual(ActionStateType.Attack, result);
        }

        [Test]
        public void CheckTransition_Attack_NoVictim_CanMove_ReturnsMove()
        {
            var result = _service.CheckTransition(
                ActionStateType.Attack, health: 100f, hasVictim: false, canMove: true);

            Assert.AreEqual(ActionStateType.Move, result);
        }

        [Test]
        public void CheckTransition_Attack_NoVictim_CannotMove_ReturnsIdle()
        {
            var result = _service.CheckTransition(
                ActionStateType.Attack, health: 100f, hasVictim: false, canMove: false);

            Assert.AreEqual(ActionStateType.Idle, result);
        }

        [Test]
        public void CheckTransition_Idle_NoVictim_StaysIdle()
        {
            var result = _service.CheckTransition(
                ActionStateType.Idle, health: 100f, hasVictim: false, canMove: false);

            Assert.AreEqual(ActionStateType.Idle, result);
        }

        [Test]
        public void CheckTransition_Attack_HasVictim_StaysAttack()
        {
            var result = _service.CheckTransition(
                ActionStateType.Attack, health: 100f, hasVictim: true, canMove: false);

            Assert.AreEqual(ActionStateType.Attack, result);
        }

        // ── CheckTransition: Downed 우선순위 ──

        [Test]
        public void CheckTransition_Idle_HasVictim_ButHealthZero_ReturnsDowned()
        {
            // 타겟이 있어도 체력이 0이면 Downed가 우선
            var result = _service.CheckTransition(
                ActionStateType.Idle, health: 0f, hasVictim: true, canMove: false);

            Assert.AreEqual(ActionStateType.Downed, result);
        }

        // ── ResolvePhaseTransition ──

        [Test]
        public void ResolvePhaseTransition_End_ReturnsFreeze()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.End, canMove: false);

            Assert.AreEqual(ActionStateType.Freeze, result);
        }

        [Test]
        public void ResolvePhaseTransition_RoundLose_ReturnsFreeze()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.RoundLose, canMove: false);

            Assert.AreEqual(ActionStateType.Freeze, result);
        }

        [Test]
        public void ResolvePhaseTransition_BattleWin_ReturnsFreeze()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.BattleWin, canMove: false);

            Assert.AreEqual(ActionStateType.Freeze, result);
        }

        [Test]
        public void ResolvePhaseTransition_BattleLose_ReturnsFreeze()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.BattleLose, canMove: false);

            Assert.AreEqual(ActionStateType.Freeze, result);
        }

        [Test]
        public void ResolvePhaseTransition_Maintenance_ReturnsWaiting()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.Maintenance, canMove: false);

            Assert.AreEqual(ActionStateType.Waiting, result);
        }

        [Test]
        public void ResolvePhaseTransition_Combat_CanMove_ReturnsMove()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.Combat, canMove: true);

            Assert.AreEqual(ActionStateType.Move, result);
        }

        [Test]
        public void ResolvePhaseTransition_Combat_CannotMove_ReturnsIdle()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.Combat, canMove: false);

            Assert.AreEqual(ActionStateType.Idle, result);
        }

        [Test]
        public void ResolvePhaseTransition_Ready_ReturnsNull()
        {
            var result = _service.ResolvePhaseTransition(PhaseType.Ready, canMove: false);

            Assert.IsNull(result);
        }
    }
}
