using Common.Data.Damage;
using Common.Data.Skills.SkillDefinitions;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Units.Attackables;
using Scenes.Battle.Feature.Units.Attackers;
using Scenes.Battle.Feature.Units.Damage;

namespace Scenes.Battle.Feature.Unit.Skills.Executables
{
    public class FireArrowExecutor : Executable
    {
        /// <summary>스킬 정의 데이터 (계수 조회용)</summary>
        private readonly SkillDefinitionData _data;
        private readonly Attacker _attacker;
        private readonly Victim _victim;

        public FireArrowExecutor(Castable castable, SkillDefinitionData data, Attacker attacker, Victim victim)
            : base(castable)
        {
            _data = data;
            _attacker = attacker;
            _victim = victim;
        }

        protected override void Executing()
        {
            var damageCoefficient = _data.Coefficients["damage"];
            float baseDamage = SkillCoefficientCalculator.Calculate(damageCoefficient, _attacker.Unit.StatSheet);

            float damage = DamageCalculator.Calculate(
                baseDamage,
                _attacker.Unit.StatSheet,
                _victim.Unit.StatSheet,
                DamageType.Magical
            );

            _victim.Hit(damage);
            EndExecute();
        }

        protected override void EndExecuting()
        {
        }
    }
}
