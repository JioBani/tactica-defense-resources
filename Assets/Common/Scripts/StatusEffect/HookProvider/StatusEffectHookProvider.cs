// ─────────────────────────────────────────────
// StatusEffectHookProvider: 특정 훅 인터페이스를 구현한 SE를 캐싱하고
// 게임 이벤트 발생 시 해당 SE들에게 전달하는 추상 기본 클래스.
// ─────────────────────────────────────────────
using System.Collections.Generic;

namespace Common.Scripts.StatusEffect.HookProvider
{
    /// <summary>
    /// 특정 훅 인터페이스를 구현한 SE를 자동 캐싱하는 추상 기본 클래스.
    /// 서브클래스는 게임 이벤트 발생 시 StatusEffects 리스트를 순회하여 훅을 실행한다.
    /// </summary>
    /// <typeparam name="T">SE가 구현하는 훅 인터페이스 타입.</typeparam>
    public abstract class StatusEffectHookProvider<T> : IStatusEffectHookProvider where T : IStatusEffectHook
    {
        /// <summary>훅 인터페이스를 구현한 SE 목록.</summary>
        protected readonly List<T> StatusEffects = new();

        /// <summary>SE 추가 시 훅 인터페이스 구현 여부를 확인하고 캐싱한다.</summary>
        public void OnStatusEffectAdded(StatusEffect statusEffect)
        {
            if (statusEffect is T typed)
                StatusEffects.Add(typed);
        }

        /// <summary>SE 제거 시 캐시에서 제거한다.</summary>
        public void OnStatusEffectRemoved(StatusEffect statusEffect)
        {
            if (statusEffect is T typed)
                StatusEffects.Remove(typed);
        }

        /// <summary>리소스를 정리한다. 필요 시 서브클래스에서 오버라이드한다.</summary>
        public virtual void Dispose()
        {
        }
    }
}