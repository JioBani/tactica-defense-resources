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

        private bool _initialized;

        private void Awake()
        {
            actionStateController.RegisterListener(this);
            unit.OnSpawnEvent += OnSpawn;
        }

        /// <summary>유닛 소환 시 호출된다.</summary>
        private void OnSpawn(Unit spawnedUnit)
        {
            // 데이터에서 해당 성급의 OverrideController를 Animator에 할당한다.
            var controller = spawnedUnit.UnitLoadOutData.Unit.GetAnimatorByStar(spawnedUnit.StatSheet.Star);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                _initialized = true;
                // 초기 OnStateEnter는 이미 호출된 뒤이므로, 현재 상태의 애니메이션을 즉시 재생한다.
                animator.Play(actionStateController.CurrentState.ToString());
            }
            else
            {
                _initialized = false;
            }
        }

        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (_initialized)
            {
                animator.Play(stateType.ToString());
            }
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
