// ─────────────────────────────────────────────
// BruiserSynergyController: 난동꾼 시너지. 보유 유닛에 BruiserSSE를 부여한다.
// 추가 효과 없이 부모의 SSE 부여 로직만으로 동작한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Synergy.SynergyEffects;

namespace Scenes.Battle.Feature.Synergy.SynergyControllers
{
    /// <summary>
    /// 난동꾼 시너지 컨트롤러. BruiserSSE를 생성하여 부모가 시너지 보유 유닛에 부여한다.
    /// </summary>
    public class BruiserSynergyController : SynergyController
    {
        public BruiserSynergyController(SynergyActivation activation)
            : base(activation) { }

        /// <summary>BruiserSynergyStatusEffect 인스턴스를 생성한다.</summary>
        protected override SynergyStatusEffect CreateSynergyStatusEffect()
        {
            return new BruiserSynergyStatusEffect(Definition.StatusEffectDefinition);
        }
    }
}