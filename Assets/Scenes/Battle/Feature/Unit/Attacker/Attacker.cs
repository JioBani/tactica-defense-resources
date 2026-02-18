using System;
using System.Collections.Generic;
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
        private readonly List<Victim> _victimsInRange = new();

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
            if (!other.CompareTag("Victim")) return;

            Victim newVictim = other.GetComponent<Victim>();
            if (newVictim.Unit.fraction == Unit.fraction) return;

            _victimsInRange.Add(newVictim);

            if (_victim == null)
            {
                TryAcquireTarget();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Victim")) return;

            Victim exitVictim = other.GetComponent<Victim>();
            _victimsInRange.Remove(exitVictim);

            if (_victim == exitVictim)
            {
                _victim = null;
                OnTargetExit?.Invoke(exitVictim);
                TryAcquireTarget();
            }
        }

        private void TryAcquireTarget()
        {
            for (int i = _victimsInRange.Count - 1; i >= 0; i--)
            {
                var candidate = _victimsInRange[i];
                if (candidate == null ||
                    candidate.Unit.ActionStateController.CurrentState == ActionStateType.Downed)
                {
                    _victimsInRange.RemoveAt(i);
                    continue;
                }

                _victim = candidate;
                OnTargetEnter?.Invoke(_victim);
                return;
            }
        }

        private void OnDestroy()
        {
            unit.OnSpawnEvent -= SetStats;
            unit.StatSheet.AttackRange.OnChange -= OnAttackRangeChanged;
            unit.StatSheet.AttackSpeed.OnChange -= OnAttackSpeedChanged;
            _attackRepeater?.Dispose();
            _victimsInRange.Clear();
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
            if (stateType == ActionStateType.Attack && _victim != null &&
                _victim.Unit.ActionStateController.CurrentState == ActionStateType.Downed)
            {
                Victim downedVictim = _victim;
                _victim = null;
                OnTargetExit?.Invoke(downedVictim);
                TryAcquireTarget();
            }
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

    }
}
