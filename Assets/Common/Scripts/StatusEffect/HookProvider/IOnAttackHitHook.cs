// ─────────────────────────────────────────────
// IOnAttackHitHook: 공격 적중 시 SE에 전달되는 훅 인터페이스.
// SE가 이 인터페이스를 구현하면, OnAttackHitHookProvider가 자동으로 캐싱하여
// 공격 적중 이벤트 발생 시 OnAttackHit을 호출한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Units.Attackables;

namespace Common.Scripts.StatusEffect.HookProvider
{
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
