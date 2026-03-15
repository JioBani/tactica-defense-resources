using Scenes.Battle.Feature.Units.Attackers;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.Attackables
{
    public class Victim : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        public Unit Unit => unit;

        /// <summary>피해를 적용하고, 공격자의 적중 이벤트를 발행한다.</summary>
        public void Hit(float damage, Attacker attacker)
        {
            unit.StatSheet.SetCurrentHealth(unit.StatSheet.Health.Value - damage);
            attacker.NotifyAttackHit(this);
        }

        public void Hit(float damage)
        {
            unit.StatSheet.SetCurrentHealth(unit.StatSheet.Health.Value - damage);
        }
    }
}
