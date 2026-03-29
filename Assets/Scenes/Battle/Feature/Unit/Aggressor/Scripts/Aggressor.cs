using Common.Scripts.ObjectPool;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Scenes.Battle.Feature.Aggressors
{
    public class Aggressor : MonoBehaviour, IStateListener<ActionStateType>
    {
        private Rigidbody2D _rigidbody2d;

        [SerializeField] private Poolable poolable;
        [SerializeField] private ActionStateController actionStateController;

        private void Awake()
        {
            _rigidbody2d = GetComponent<Rigidbody2D>();

            // IStateListener 등록
            actionStateController.RegisterListener(this);
        }

        private void OnDisable()
        {
            _rigidbody2d.linearVelocity = Vector2.zero;
        }

        // IStateListener 명시적 구현
        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Downed)
            {
                OnDown();
            }
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType)
        {
            // Exit 단계에서는 특별한 동작 없음
        }

        private void OnDestroy()
        {
            actionStateController.UnregisterListener(this);
        }

        private void OnDown()
        {
            poolable.DeSpawn();
        }

    }
}
