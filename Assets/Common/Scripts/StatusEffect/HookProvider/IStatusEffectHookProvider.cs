// ─────────────────────────────────────────────
// IStatusEffectHookProvider: SEController에 붙는 플러그인 인터페이스.
// SE에 새로운 생명주기 훅을 제공하고, 게임 이벤트를 관심 있는 SE에 전달한다.
// ─────────────────────────────────────────────
using System;

namespace Common.Scripts.StatusEffect.HookProvider
{
    /// <summary>
    /// SEController에 붙는 플러그인 인터페이스.
    /// SE 추가/제거 알림을 받아 관심 인터페이스를 캐싱하거나 해제한다.
    /// </summary>
    public interface IStatusEffectHookProvider : IDisposable
    {
        /// <summary>SE가 컨트롤러에 추가되었을 때 호출된다.</summary>
        void OnStatusEffectAdded(StatusEffect statusEffect);

        /// <summary>SE가 컨트롤러에서 제거되었을 때 호출된다.</summary>
        void OnStatusEffectRemoved(StatusEffect statusEffect);
    }
}