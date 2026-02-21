using Common.Data.Skills.SkillDefinitions;
using Common.Scripts.BubbleMessage;
using Scenes.Battle.Feature.Projectiles;
using Scenes.Battle.Feature.Unit.Skills.Executables;
using Scenes.Battle.Feature.Unit.Skills.Skills;
using Scenes.Battle.Feature.Units.Attackers;

namespace Scenes.Battle.Feature.Unit.Skills.Castables
{
    public class FireArrow : SkillCast
    {
        /// <summary>스킬 정의 데이터 (계수 등 SO에서 읽어온 데이터)</summary>
        private readonly SkillDefinitionData _data;
        private readonly Attacker _attacker;

        public FireArrow(SkillDefinitionData data, Attacker attacker)
        {
            _data = data;
            _attacker = attacker;
        }

        public override bool CanCast()
        {
            return _attacker.Victim;
        }

        public override Executable Casting()
        {
            var executor = new FireArrowExecutor(this, _data, _attacker, _attacker.Victim);

            var projectile = ProjectileGenerator.Instance.Generate();

            projectile.OnHit += executor.Execute;

            projectile.Shot(_attacker.transform, _attacker.Victim.transform);

            BubbleMessageSpawner.Instance.SpawnAtWorld("불화살", _attacker.transform.position);

            return executor;
        }
    }
}
