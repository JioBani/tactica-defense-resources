// ─────────────────────────────────────────────
// IStatusEffectHook: SE 훅 인터페이스의 마커.
// 새 훅 인터페이스를 정의할 때 이 인터페이스를 상속한다.
// ─────────────────────────────────────────────
namespace Common.Scripts.StatusEffect.HookProvider
{
    /// <summary>
    /// SE 훅 인터페이스의 마커. 새 훅 인터페이스를 정의할 때 상속한다.
    /// StatusEffectHookProvider의 제네릭 제약으로 사용된다.
    /// </summary>
    public interface IStatusEffectHook
    {
    }
}