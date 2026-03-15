// ─────────────────────────────────────────────
// OnAttackHitHookProvider: Attacker의 공격 적중 이벤트를 구독하고,
// IOnAttackHitHook을 구현한 SE들에게 전달한다.
// DefenderStatusEffectController에서 등록한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Units.Attackables;
using Scenes.Battle.Feature.Units.Attackers;

namespace Common.Scripts.StatusEffect.HookProvider
{
    /// <summary>
    /// Attacker의 공격 적중 이벤트를 IOnAttackHitHook SE들에게 전달하는 Provider.
    /// </summary>
    public class OnAttackHitHookProvider : StatusEffectHookProvider<IOnAttackHitHook>
    {
        private readonly Attacker _attacker;

        public OnAttackHitHookProvider(Attacker attacker)
        {
            _attacker = attacker;
            _attacker.OnAttackHitEvent += HandleAttackHit;
        }

        /// <summary>공격 적중 시 캐싱된 SE들에게 훅을 전달한다.</summary>
        private void HandleAttackHit(Victim victim)
        {
            foreach (var hook in StatusEffects)
                hook.OnAttackHit(victim);
        }

        public override void Dispose()
        {
            if (_attacker != null)
                _attacker.OnAttackHitEvent -= HandleAttackHit;
        }
    }
}
