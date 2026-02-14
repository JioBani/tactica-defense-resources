using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Scenes.Battle.Feature.Units
{
    public class Mover : MonoBehaviour, IStateListener<ActionStateType>
    {
        [SerializeField] private ActionStateController actionStateController;
        [SerializeField] private float speed;
        [SerializeField] private Feature.Units.Unit unit;

        private Rigidbody2D _rigidbody2D;

        private void Awake()
        {
            unit.OnSpawnEvent += SetSpeed;
            _rigidbody2D = GetComponent<Rigidbody2D>();

            // IStateListener 등록
            actionStateController.RegisterListener(this);
        }

        private void SetSpeed(Feature.Units.Unit unit)
        {
            speed = unit.StatSheet.MoveSpeed.CurrentValue;
            unit.StatSheet.MoveSpeed.OnChange += OnMoveSpeedChanged;
        }

        private void OnMoveSpeedChanged(float value)
        {
            speed = value;
        }

        private void Move()
        {
            _rigidbody2D.linearVelocity = Vector2.down * speed;
        }

        private void Stop()
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
        }

        // IStateListener 명시적 구현
        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Move)
            {
                Move();
            }
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Move)
            {
                Stop();
            }
        }

        private void OnDestroy()
        {
            unit.OnSpawnEvent -= SetSpeed;
            unit.StatSheet.MoveSpeed.OnChange -= OnMoveSpeedChanged;
            actionStateController.UnregisterListener(this);
        }
    }
}
