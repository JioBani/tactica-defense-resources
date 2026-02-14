using System;
using Common.Scripts.DynamicRepeater;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Unit.Attackers.AttackContexts;
using Scenes.Battle.Feature.Unit.Attackers.AttackContexts.Dtos;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Units.Attackables;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.Attackers
{
    public class Attacker : MonoBehaviour, IStateListener<ActionStateType>
    {
        [SerializeField] private float range;
        [SerializeField] private float attackSpeed;
        [SerializeField] private Unit unit;
        [SerializeField] private ActionStateController actionStateController;
        public Unit Unit => unit;

        private CircleCollider2D _circleCollider2D;
        private Victim _victim;
        public Victim Victim => _victim;

        public Action<Victim> OnTargetEnter;
        public Action<Victim> OnTargetExit;

        private DynamicRepeater _attackRepeater;
        private AttackContextDto _attackContextDto;

        private AttackCast _attackCast;

        private void Awake()
        {
            _circleCollider2D = GetComponent<CircleCollider2D>();
            unit.OnSpawnEvent += SetStats;

            // IStateListener 등록
            actionStateController.RegisterListener(this);

            _attackCast = new AttackCast(this);
        }

        // Update는 더 이상 필요 없음 - 상태 전환 로직이 ActionStateController로 이동

        private void SetStats(Unit unit)
        {
            _circleCollider2D.radius = unit.StatSheet.AttackRange.CurrentValue;
            attackSpeed = unit.StatSheet.AttackSpeed.CurrentValue;

            unit.StatSheet.AttackRange.OnChange += OnAttackRangeChanged;
            unit.StatSheet.AttackSpeed.OnChange += OnAttackSpeedChanged;

            _attackRepeater?.Dispose();
            _attackRepeater = new DynamicRepeater(
                intervalNow: () => TimeSpan.FromSeconds(1 / attackSpeed),
                job : async () => Attack()
            );
        }

        private void OnAttackRangeChanged(float value)
        {
            _circleCollider2D.radius = value;
        }

        private void OnAttackSpeedChanged(float value)
        {
            attackSpeed = value;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_victim == null && other.CompareTag("Victim"))
            {
                Victim newVictim = other.GetComponent<Victim>();

                if (
                    newVictim.Unit.fraction != Unit.fraction && 
                    newVictim.Unit.ActionStateController.CurrentState != ActionStateType.Downed
                )
                {
                    _victim = newVictim;
                    OnTargetEnter?.Invoke(_victim);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (
                _victim != null && 
                other.CompareTag("Victim") && 
                _victim == other.GetComponent<Victim>()
            )
            {
                ReleaseVictim();
            }
        }

        private void OnDestroy()
        {
            unit.OnSpawnEvent -= SetStats;
            unit.StatSheet.AttackRange.OnChange -= OnAttackRangeChanged;
            unit.StatSheet.AttackSpeed.OnChange -= OnAttackSpeedChanged;
            _attackRepeater?.Dispose();
            actionStateController.UnregisterListener(this);
        }

        // IStateListener 명시적 구현
        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Attack)
            {
                StartRepeat();
            }
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Attack)
            {
                EndAttackRepeat();
            }
        }

        private void StartRepeat()
        {
            _attackRepeater.Start();
        }

        private void EndAttackRepeat()
        {
            _attackRepeater.Stop();
        }

        private void Attack()
        {
            if (_victim != null)
            {
                _attackCast.Cast();
                
                // AttackContextDto attackContextDto = new AttackContextDto(
                //     damage: unit.StatSheet.PhysicalAttack.CurrentValue,
                //     attacker: this,
                //     victim: _victim
                // );
                //
                // var context = AttackContextFactory.Instance.GenerateRanged(attackContextDto);
                //
                // context.TryAttack();
            }
        }

        private void ReleaseVictim()
        {
            Victim exitVictim = _victim;
            _victim = null;
            OnTargetExit?.Invoke(exitVictim);
        }
    }
}
