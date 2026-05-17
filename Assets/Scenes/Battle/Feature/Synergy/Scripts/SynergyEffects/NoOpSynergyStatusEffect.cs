// ─────────────────────────────────────────────
// NoOpSynergyStatusEffect: 테스트용 SummonTrait 시너지에 부여되는 비효과 SE.
// TACD-300 결정에 따라 SummonTrait의 StatusEffectDefinitionData 효과 필드가 비어 있어
// 카운트·티어 추적은 필요하지만 스탯/상태 변화는 일으키지 않는 placeholder 용도.
// 실제 효과를 가진 SummonTrait 시너지가 추가되는 시점에는 본 클래스를 사용하지 말고,
// 기존 SummonerEffect 시너지 패턴(BruiserSynergyStatusEffect 등)처럼 시너지별 전용
// SynergyStatusEffect 서브클래스를 구현한다.
// ─────────────────────────────────────────────
using Common.Data.StatusEffects;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>SummonTrait 시너지용 비효과 SE placeholder. 카운트·티어 추적만 동작하고 스탯 변화는 일으키지 않는다.</summary>
    public class NoOpSynergyStatusEffect : SynergyStatusEffect<SynergyStatusEffectContext>
    {
        public NoOpSynergyStatusEffect(StatusEffectDefinitionData definition) : base(definition) { }
    }
}
