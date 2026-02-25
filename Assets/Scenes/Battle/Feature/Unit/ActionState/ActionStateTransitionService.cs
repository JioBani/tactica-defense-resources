// ─────────────────────────────────────────────
// ActionStateTransitionService: ActionStateController의 상태 전환 판정 로직을 담당한다.
// MonoBehaviour에서 분리된 순수 C# 클래스로, 유닛 테스트가 가능하다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Rounds.Phases;

namespace Scenes.Battle.Feature.Units.ActionStates
{
    public class ActionStateTransitionService
    {
        /// <summary>
        /// 매 프레임 호출되어 현재 상태에서 전환할 다음 상태를 판정한다.
        /// 전환 조건이 없으면 currentState를 그대로 반환한다.
        /// </summary>
        public ActionStateType CheckTransition(
            ActionStateType currentState, float health, bool hasVictim, bool canMove)
        {
            // 우선순위 1: 전투 중(Idle/Move/Attack) 체력이 0 이하면 Downed 전환
            if (health <= 0
                && currentState is ActionStateType.Idle or ActionStateType.Move or ActionStateType.Attack)
            {
                return ActionStateType.Downed;
            }

            // 우선순위 2: 각 상태별 전환 조건 체크
            switch (currentState)
            {
                case ActionStateType.Idle:
                case ActionStateType.Move:
                    if (hasVictim) return ActionStateType.Attack;
                    break;

                case ActionStateType.Attack:
                    if (!hasVictim) return canMove ? ActionStateType.Move : ActionStateType.Idle;
                    break;

                case ActionStateType.Downed:
                case ActionStateType.Freeze:
                case ActionStateType.Waiting:
                    break;
            }

            return currentState;
        }

        /// <summary>
        /// Phase 전환 시 대응하는 ActionState를 반환한다.
        /// 전환이 불필요한 Phase(Ready 등)에는 null을 반환한다.
        /// </summary>
        public ActionStateType? ResolvePhaseTransition(PhaseType phaseType, bool canMove)
        {
            return phaseType switch
            {
                PhaseType.End
                    or PhaseType.RoundLose
                    or PhaseType.BattleWin
                    or PhaseType.BattleLose => ActionStateType.Freeze,

                PhaseType.Maintenance => ActionStateType.Waiting,
                PhaseType.Combat => canMove ? ActionStateType.Move : ActionStateType.Idle,
                _ => null,
            };
        }
    }
}
