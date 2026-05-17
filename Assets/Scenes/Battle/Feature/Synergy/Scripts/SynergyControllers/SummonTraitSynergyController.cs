// ─────────────────────────────────────────────
// SummonTraitSynergyController: 테스트용 SummonTrait 시너지 통합 컨트롤러.
// 현재 SummonTrait 8개(TACD-300)가 모두 NoOp SSE로 동작 동일하여 시너지별 분리 없이
// 단일 클래스로 흡수한 placeholder 구현.
//
// 주의 — 실제 효과를 가진 SummonTrait 시너지가 추가될 때는 본 통합 클래스를 따르지 말고,
// 기존 SummonerEffect 시너지 패턴(BruiserSynergyController / ArcanistSynergyController 등)처럼
// 각 SummonTrait별로 SynergyController를 상속한 전용 컨트롤러 + 전용 SynergyStatusEffect
// 서브클래스를 만들고 SynergyControllerFactory의 SynergyId switch에 케이스를 추가한다.
// (SynergyControllerFactory는 현재 SynergyType.SummonTrait 시 본 통합 클래스로 라우팅하므로,
//  per-synergy 분기 도입 시 그 분기 로직도 함께 갱신 필요.)
// ─────────────────────────────────────────────
namespace Scenes.Battle.Feature.Synergy.SynergyControllers
{
    /// <summary>SummonTrait 시너지의 카운트·티어 추적을 처리하는 통합 컨트롤러.</summary>
    public class SummonTraitSynergyController : SynergyController
    {
        public SummonTraitSynergyController(SynergyActivation activation) : base(activation) { }

        /// <summary>비효과 SE를 생성한다. SummonTrait는 SE 효과가 비어있는 placeholder.</summary>
        protected override SynergyStatusEffect CreateSynergyStatusEffect()
            => new NoOpSynergyStatusEffect(Definition.StatusEffectDefinition);
    }
}
