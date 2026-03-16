// ─────────────────────────────────────────────
// AttackHitHookBinder + OnAttackHitHookProvider + IOnAttackHitHook
// Attacker의 공격 적중 이벤트를 IOnAttackHitHook SE들에게 전달한다.
// 프리팹의 HookProviders 오브젝트에 AttackHitHookBinder를 부착하여 등록한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Units.Attackables;
using Scenes.Battle.Feature.Units.Attackers;
using UnityEngine;

namespace Common.Scripts.StatusEffect.HookProvider
{
    /// <summary>
    /// OnAttackHitHookProvider를 StatusEffectController에 등록하는 바인더 컴포넌트.
    /// 프리팹의 HookProviders 오브젝트에 부착한다.
    /// </summary>
    public class AttackHitHookBinder : MonoBehaviour
    {
        [SerializeField] private StatusEffectController statusEffectController;
        [SerializeField] private Attacker attacker;

        private void Awake()
        {
            if (statusEffectController != null && attacker != null)
                statusEffectController.AddHookProvider(new OnAttackHitHookProvider(attacker));
        }
    }

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

    /// <summary>
    /// 공격 적중 시 호출되는 SE 훅 인터페이스.
    /// </summary>
    public interface IOnAttackHitHook : IStatusEffectHook
    {
        /// <summary>공격이 적중했을 때 호출된다.</summary>
        /// <param name="victim">피격된 대상.</param>
        void OnAttackHit(Victim victim);
    }
}
