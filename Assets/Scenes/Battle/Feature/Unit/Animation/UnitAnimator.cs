using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Scenes.Battle.Feature.Units
{
    /// <summary>ActionState 변경 시 애니메이션을 자동 전환하는 컴포넌트.</summary>
    public class UnitAnimator : MonoBehaviour, IStateListener<ActionStateType>
    {
        [SerializeField] private Animator animator;
        [SerializeField] private ActionStateController actionStateController;
        [SerializeField] private Unit unit;

        private void Awake()
        {
            actionStateController.RegisterListener(this);
            unit.OnSpawnEvent += OnSpawn;
        }

        /// <summary>유닛 소환 시 호출된다.</summary>
        private void OnSpawn(Unit spawnedUnit)
        {
            // 데이터에서 해당 성급의 OverrideController를 Animator에 할당한다.
            animator.runtimeAnimatorController =
                spawnedUnit.UnitLoadOutData.Unit.GetAnimatorByStar(spawnedUnit.StatSheet.Star);
        }

        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            animator.Play(stateType.ToString());
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType)
        {
        }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType)
        {
        }

        private void OnDestroy()
        {
            actionStateController.UnregisterListener(this);
            unit.OnSpawnEvent -= OnSpawn;
        }
    }
}
