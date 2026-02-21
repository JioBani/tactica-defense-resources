using Common.Data.Damage;
using Scenes.Battle.Feature.Unit.Skills.Castables;
using Scenes.Battle.Feature.Units.Attackables;
using Scenes.Battle.Feature.Units.Attackers;
using Scenes.Battle.Feature.Units.Damage;

namespace Scenes.Battle.Feature.Unit.Skills.Executables
{
    public class FireArrowExecutor : Executable
    {
        private Attacker _attacker;
        private Victim _victim;
        
        public FireArrowExecutor(Castable castable, Attacker attacker, Victim victim) : base(castable)
        {
            _attacker = attacker;
            _victim = victim;
        }

        protected override void Executing()
        {
            float damage = DamageCalculator.Calculate(
                _attacker.Unit.StatSheet,
                _victim.Unit.StatSheet,
                DamageType.Magical,
                skillCoefficient: 1.5f
            );
            _victim.Hit(damage);
            EndExecute();
        }

        protected override void EndExecuting()
        {
            
        }
    }
}