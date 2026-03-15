// ─────────────────────────────────────────────
// FreljordFrostEffect: 프렐요드 시너지로 인한 서리 둔화 효과.
// 피해자의 StatusEffectController에 부여되어 MoveSpeed를 감소시킨다.
// 지속시간이 만료되면 자동 제거된다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Units.UnitStats;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy.SynergyEffects
{
    /// <summary>
    /// 프렐요드 서리 둔화 SE. 피해자의 MoveSpeed에 % 감소 수정자를 적용한다.
    /// 지속시간 만료 시 자동 제거된다.
    /// </summary>
    public class FreljordFrostEffect : StatusEffect
    {
        private float _slowPercent;
        private float _remainingTime;
        private UnitStat _moveSpeed;

        public FreljordFrostEffect(StatusEffectDefinitionData definition, float slowPercent, float duration)
            : base(definition)
        {
            _slowPercent = slowPercent;
            _remainingTime = duration;
        }

        public override void OnApply(StatusEffectContext context)
        {
            var frostContext = (FreljordFrostEffectContext)context;
            _moveSpeed = frostContext.MoveSpeed;
            ApplyModifier();
        }

        public override void OnUpdate()
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
                RequestRemove();
        }

        public override void OnRemove()
        {
            _moveSpeed?.RemoveModifiersBySource(this);
        }

        /// <summary>지속시간을 리셋한다. 동일 공격자의 재적중 시 호출.</summary>
        public void Refresh(float duration)
        {
            _remainingTime = duration;
        }

        /// <summary>둔화 수치를 갱신한다. 티어 변경 시 호출.</summary>
        public void UpdateSlowPercent(float newSlowPercent)
        {
            _moveSpeed?.RemoveModifiersBySource(this);
            _slowPercent = newSlowPercent;
            ApplyModifier();
        }

        /// <summary>MoveSpeed에 둔화 수정자를 추가한다.</summary>
        private void ApplyModifier()
        {
            _moveSpeed.AddModifier(new StatModifier(this, StatModifierType.Percent, -_slowPercent));
        }
    }

    /// <summary>FreljordFrostEffect Apply 시 전달되는 Context.</summary>
    public class FreljordFrostEffectContext : StatusEffectContext
    {
        /// <summary>둔화 대상의 MoveSpeed 스탯.</summary>
        public UnitStat MoveSpeed { get; }

        public FreljordFrostEffectContext(UnitStat moveSpeed)
        {
            MoveSpeed = moveSpeed;
        }
    }
}
