using Common.Data.Damage;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Units.Attackables;
using Scenes.Battle.Feature.Units.Attackers;
using Scenes.Battle.Feature.Units.Damage;

namespace Scenes.Battle.Feature.Unit.Skills.Executables
{
    public class RangeAttackExecutor : Executable
    {
        private readonly Attacker _attacker;
        private readonly Victim _victim;

        public RangeAttackExecutor(Castable castable, Attacker attacker, Victim victim) : base(castable)
        {
            _attacker = attacker;
            _victim = victim;
        }

        protected override void Executing()
        {
            float damage = DamageCalculator.Calculate(
                _attacker.Unit.StatSheet,
                _victim.Unit.StatSheet,
                DamageType.Physical
            );
            _victim.Hit(damage);
        }

        protected override void EndExecuting()
        {
            
        }
    }
}