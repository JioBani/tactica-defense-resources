using System;
using Common.Data.Units.UnitStatsByLevel;
using Common.Scripts.DynamicRepeater;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Units.Attackables;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Skills.Caster
{
    public class AttackCaster : MonoBehaviour, IStateListener<ActionStateType>
    {
        [SerializeField] private float range;
        [SerializeField] private float attackSpeed;
        [SerializeField] private Units.Unit unit;
        [SerializeField] private ActionStateController actionStateController;
        public Units.Unit Unit => unit;

        private CircleCollider2D _circleCollider2D;
        private Victim _victim;
        public Victim Victim => _victim;

        public Action<Victim> OnTargetEnter;
        public Action<Victim> OnTargetExit;

        private DynamicRepeater _attackRepeater;
        private AttackCast _attackCast;

        private void Awake()
        {
            _circleCollider2D = GetComponent<CircleCollider2D>();
            unit.OnSpawnEvent += SetStats;

            // IStateListener 등록
            actionStateController.RegisterListener(this);

            //_attackCast = new AttackCast(this);
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

        private void Update()
        {
            if (_victim && _victim.Unit.ActionStateController.CurrentState == ActionStateType.Downed)
            {
                ReleaseVictim();
            }
        }

        //TODO: 동적 스탯 변경을 적용하기
        private void SetStats(Units.Unit unit)
        {
            _circleCollider2D.radius = unit.UnitLoadOutData.Stats.GetStat(UnitStatKind.AttackRange, 0);
            attackSpeed = unit.UnitLoadOutData.Stats.GetStat(UnitStatKind.AttackSpeed, 0);
            
            _attackRepeater?.Dispose();
            _attackRepeater = new DynamicRepeater(
                intervalNow: () => TimeSpan.FromSeconds(1 / attackSpeed), 
                job : async () => Attack()
            );
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
            _attackRepeater?.Dispose();
            actionStateController.UnregisterListener(this);
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