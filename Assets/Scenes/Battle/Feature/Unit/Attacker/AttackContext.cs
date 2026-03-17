// ─────────────────────────────────────────────
// AttackContext: 공격 적중 정보를 담는 데이터 클래스.
// Victim.Hit에 전달되며, HookProvider를 통해 SE에도 전달된다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Units.Attackers;

namespace Scenes.Battle.Feature.Units.Attackables
{
    // TODO: 논타겟 스킬(범위 공격 등)의 경우 Victim이 null일 수 있다.
    //       Victim을 nullable로 변경하거나, 논타겟용 컨텍스트를 별도로 분리하는 리팩토링이 필요하다.

    /// <summary>
    /// 공격 적중 시 전달되는 컨텍스트.
    /// Source가 null이면 일반 공격, non-null이면 SE 등이 유발한 추가 피해.
    /// </summary>
    public class AttackContext
    {
        /// <summary>적용할 피해량.</summary>
        public float Damage { get; }

        /// <summary>공격을 수행한 Attacker.</summary>
        public Attacker Attacker { get; }

        /// <summary>피격 대상.</summary>
        public Victim Victim { get; }

        /// <summary>이 공격을 유발한 소스. null이면 일반 공격.</summary>
        public object Source { get; }

        public AttackContext(float damage, Attacker attacker, Victim victim, object source = null)
        {
            Damage = damage;
            Attacker = attacker;
            Victim = victim;
            Source = source;
        }
    }
}
