// ─────────────────────────────────────────────
// UnitAnimator: Unit 소환 시 애니메이션 컨트롤러를 할당하고,
// 공격속도에 따라 Attack 애니메이션 속도를 자동 조절한다.
// 확장 참고: ActionStateType 기반이지만 Unit 비종속 애니메이터가 필요하면,
// ActionStateAnimator : StateAnimator<ActionStateType> 중간 클래스를 도입하고
// UnitAnimator : ActionStateAnimator 로 변경한다.
// ─────────────────────────────────────────────
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Scenes.Battle.Feature.Units
{
    /// <summary>Unit의 ActionState에 연동하여 애니메이션을 자동 전환하는 컴포넌트.</summary>
    public class UnitAnimator : StateAnimator<ActionStateType>
    {
        [SerializeField] private Unit unit;

        private float _attackSpeed;

        protected override void Awake()
        {
            base.Awake();
            unit.OnSpawnEvent += OnSpawn;
        }

        /// <summary>유닛 소환 시 호출된다.</summary>
        private void OnSpawn(Unit spawnedUnit)
        {
            var controller = spawnedUnit.UnitLoadOutData.Unit.GetAnimatorByStar(spawnedUnit.StatSheet.Star);
            Initialize(controller);

            _attackSpeed = spawnedUnit.StatSheet.AttackSpeed.CurrentValue;
            spawnedUnit.StatSheet.AttackSpeed.OnChange += OnAttackSpeedChanged;
            SetTargetDuration(ActionStateType.Attack, 1f / _attackSpeed);
        }

        /// <summary>공격속도 변경 시 호출된다.</summary>
        private void OnAttackSpeedChanged(float value)
        {
            _attackSpeed = value;
            SetTargetDuration(ActionStateType.Attack, 1f / _attackSpeed);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            unit.OnSpawnEvent -= OnSpawn;
            unit.StatSheet.AttackSpeed.OnChange -= OnAttackSpeedChanged;
        }
    }
}
