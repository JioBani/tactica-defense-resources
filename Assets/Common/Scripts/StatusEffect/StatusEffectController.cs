// ─────────────────────────────────────────────
// StatusEffectController: MonoBehaviour. SE 컬렉션과 생명주기를 관리한다.
// 서브클래스가 Awake에서 자기 유닛 유형에 맞는 HookProvider 조합을 등록한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Scripts.SafeIterationList;
using Common.Scripts.StatusEffect.HookProvider;
using UnityEngine;

namespace Common.Scripts.StatusEffect
{
    /// <summary>
    /// MonoBehaviour. SE 컬렉션과 생명주기를 관리한다.
    /// 서브클래스가 Awake 등에서 AddHookProvider로 HookProvider를 등록한다.
    /// </summary>
    public class StatusEffectController : MonoBehaviour
    {
        private readonly SafeIterationList<StatusEffect> _statusEffects = new();
        private readonly List<IStatusEffectHookProvider> _hookProviders = new();

        /// <summary>인스펙터에서 현재 적용 중인 SE 목록을 확인하기 위한 디버그 리스트.</summary>
        [SerializeField] private List<string> debugActiveEffects = new();

        /// <summary>
        /// SE를 부여한다. OnApply 호출 후 HookProvider에 알림한다.
        /// </summary>
        public void Apply(StatusEffect statusEffect, StatusEffectContext context)
        {
            statusEffect.Controller = this;
            _statusEffects.Add(statusEffect);
            statusEffect.OnApply(context);
            debugActiveEffects.Add(statusEffect.GetType().Name);

            foreach (var hookProvider in _hookProviders)
                hookProvider.OnStatusEffectAdded(statusEffect);
        }

        /// <summary>
        /// SE를 즉시 제거한다. OnRemove 호출 후 HookProvider에 알림한다.
        /// </summary>
        public void RemoveImmediate(StatusEffect statusEffect)
        {
            statusEffect.OnRemove();

            foreach (var hookProvider in _hookProviders)
                hookProvider.OnStatusEffectRemoved(statusEffect);

            _statusEffects.Remove(statusEffect);
            debugActiveEffects.Remove(statusEffect.GetType().Name);
            statusEffect.Controller = null;
        }

        /// <summary>HookProvider를 등록한다.</summary>
        protected void AddHookProvider(IStatusEffectHookProvider hookProvider)
        {
            _hookProviders.Add(hookProvider);
        }

        /// <summary>여러 HookProvider를 한 번에 등록한다.</summary>
        protected void AddHookProviders(params IStatusEffectHookProvider[] hookProviders)
        {
            foreach (var hookProvider in hookProviders)
                _hookProviders.Add(hookProvider);
        }

        /// <summary>HookProvider를 해제한다.</summary>
        protected void RemoveHookProvider(IStatusEffectHookProvider hookProvider)
        {
            _hookProviders.Remove(hookProvider);
        }

        private void Update()
        {
            foreach (var statusEffect in _statusEffects)
            {
                statusEffect.OnUpdate();
            }

            RemoveExpiredEffects();
        }

        /// <summary>IsExpired == true인 SE를 일괄 RemoveImmediate한다.</summary>
        private void RemoveExpiredEffects()
        {
            foreach (var statusEffect in _statusEffects)
            {
                if (statusEffect.IsExpired)
                    RemoveImmediate(statusEffect);
            }
        }

        private void OnDestroy()
        {
            foreach (var statusEffect in _statusEffects)
            {
                statusEffect.OnRemove();
                statusEffect.Controller = null;
            }

            foreach (var hookProvider in _hookProviders)
                hookProvider.Dispose();
        }
    }
}