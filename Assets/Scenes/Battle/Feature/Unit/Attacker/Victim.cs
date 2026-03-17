using Scenes.Battle.Feature.Units.Attackers;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.Attackables
{
    // TODO: 필요 시 OnGetHit 이벤트를 추가하여 피격 사실을 외부에 알린다.
    //       Attacker.OnAttackHitEvent는 "공격 주체가 공격에 성공했음"을 알리는 이벤트이고,
    //       Victim.OnGetHit은 "자신이 피격당했음"을 알리는 이벤트로 역할을 분리한다.
    //       (예: 피격 시 반격, 가시 효과, 피격 VFX 등에 활용)

    public class Victim : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        public Unit Unit => unit;

        /// <summary>AttackContext를 받아 피해를 적용하고, 공격자의 적중 이벤트를 발행한다.</summary>
        public void Hit(AttackContext context)
        {
            unit.StatSheet.SetCurrentHealth(unit.StatSheet.Health.Value - context.Damage);
            context.Attacker?.NotifyAttackHit(context);
        }
    }
}
