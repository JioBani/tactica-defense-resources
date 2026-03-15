// ─────────────────────────────────────────────
// DefenderStatusEffectController: Defender용 SE 컨트롤러.
// OnAttackHitHookProvider 등을 Awake에서 등록한다.
// ─────────────────────────────────────────────
using Common.Scripts.StatusEffect;
using Common.Scripts.StatusEffect.HookProvider;
using Scenes.Battle.Feature.Units.Attackers;
using UnityEngine;

namespace Scenes.Battle.Feature.Units
{
    /// <summary>
    /// Defender용 SE 컨트롤러. 공격 적중 훅 등 Defender에 필요한 HookProvider를 등록한다.
    /// </summary>
    public class DefenderStatusEffectController : StatusEffectController
    {
        [SerializeField] private Attacker attacker;

        private void Awake()
        {
            if (attacker != null)
                AddHookProvider(new OnAttackHitHookProvider(attacker));
        }
    }
}
