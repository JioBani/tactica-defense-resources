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

        protected override ActionStateType CheckStateTransition(ActionStateType currentState)
        {
            // 우선순위 1: 전투 중(Idle/Move/Attack) 체력이 0 이하면 Downed 전환
            if (self.StatSheet.Health <= 0
                && currentState is ActionStateType.Idle or ActionStateType.Move or ActionStateType.Attack)
            {
                return ActionStateType.Downed;
            }

            // 우선순위 2: 각 상태별 전환 조건 체크
            switch (currentState)
            {
                case ActionStateType.Idle:
                    // Idle -> Attack: 타겟이 있으면 공격
                    if (attacker.Victim)
                    {
                        return ActionStateType.Attack;
                    }
                    break;

                case ActionStateType.Move:
                    // Move -> Attack: 타겟이 있으면 공격
                    if (attacker.Victim)
                    {
                        return ActionStateType.Attack;
                    }
                    break;

                case ActionStateType.Attack:
                    // Attack -> Idle/Move: 타겟이 없으면 복귀 (다운 타겟은 Attacker가 자체 해제)
                    if (!attacker.Victim)
                    {
                        return canMove ? ActionStateType.Move : ActionStateType.Idle;
                    }
                    break;

                case ActionStateType.Downed:
                    // Downed 상태는 전환 없음
                    break;

                case ActionStateType.Freeze:
                    // Freeze 상태는 전환 없음
                    break;

                case ActionStateType.Waiting:
                    // Waiting 상태는 전환 없음
                    break;
            }

            return currentState;
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
            switch (phaseType)
            {
                case PhaseType.End:
                case PhaseType.RoundLose:
                case PhaseType.BattleWin:
                case PhaseType.BattleLose:
                    RequestStateChange(ActionStateType.Freeze);
                    break;
                case PhaseType.Maintenance:
                    RequestStateChange(ActionStateType.Waiting);
                    break;
                case PhaseType.Combat:
                    RequestStateChange(canMove ? ActionStateType.Move : ActionStateType.Idle);
                    break;
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType) { }
        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType) { }
    }
}