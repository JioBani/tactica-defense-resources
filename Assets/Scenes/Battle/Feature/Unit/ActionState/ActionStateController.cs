// ─────────────────────────────────────────────
// ActionStateController: 유닛의 행동 상태(Idle/Move/Attack/Downed/Freeze/Waiting) 전환을 관리한다.
// - 상태 전환 조건과 Phase 연동은 이 클래스 내부에서만 작성한다.
// - 상태 변화에 따른 동작(HP 회복, 외형 변경 등)은 외부에서 IStateListener<ActionStateType>를 구독하여 처리한다.
// ─────────────────────────────────────────────
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Units.Attackers;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.ActionStates
{
    public class ActionStateController : StateBaseController<ActionStateType>, IStateListener<PhaseType>
    {
        [SerializeField] private Unit self;
        [SerializeField] private Attacker attacker;
        [SerializeField] private bool canMove;

        private readonly ActionStateTransitionService _transitionService = new();

        protected override ActionStateType CheckStateTransition(ActionStateType currentState)
        {
            return _transitionService.CheckTransition(
                currentState, self.StatSheet.Health, attacker.Victim, canMove);
        }

        private void OnEnable()
        {
            RoundManager.Instance.RegisterListener(this);
            StartStateBase(canMove ? ActionStateType.Move : ActionStateType.Idle);
        }

        private void OnDisable()
        {
            RoundManager.Instance.UnregisterListener(this);
        }

        // ── IStateListener<PhaseType> ──

        /// <summary>
        /// Phase 전환에 따라 ActionState를 변경한다.
        /// End/RoundLose/BattleWin/BattleLose → Freeze, Maintenance → Waiting, Combat → Idle/Move
        /// </summary>
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            var target = _transitionService.ResolvePhaseTransition(phaseType, canMove);
            if (target.HasValue) RequestStateChange(target.Value);
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType) { }
        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType) { }
    }
}