using UnityEngine;

namespace Scenes.Battle.Feature.Units.Attackables
{
    public class Victim : MonoBehaviour
    {
        [SerializeField] private Unit unit;
        public Unit Unit => unit;

        public void Hit(AttackContext context)
        {
            unit.StatSheet.Health -= context.damage;
        }

        public void Hit(float damage)
        {
            unit.StatSheet.Health -= damage;
        }
    }
}
