using Common.Scripts.BubbleMessage;
using Scenes.Battle.Feature.Projectiles;
using Scenes.Battle.Feature.Unit.Skills.Executables;
using Scenes.Battle.Feature.Unit.Skills.Skills;
using Scenes.Battle.Feature.Units.Attackers;

namespace Scenes.Battle.Feature.Unit.Skills.Castables
{
    public class FireArrow : SkillCast
    {
        private Attacker _attacker;
        public FireArrow(Attacker attacker)
        {
            _attacker = attacker;
        }

        public override bool CanCast()
        {
            return _attacker.Victim;
        }

        public override Executable Casting()
        {
            FireArrowExecutor executor = new FireArrowExecutor(this, _attacker, _attacker.Victim);

            var projectile = ProjectileGenerator.Instance.Generate();

            projectile.OnHit += executor.Execute;

            projectile.Shot(_attacker.transform, _attacker.Victim.transform);

            BubbleMessageSpawner.Instance.SpawnAtWorld("불화살", _attacker.transform.position);

            return new FireArrowExecutor(this, _attacker, _attacker.Victim);
        }
    }
}