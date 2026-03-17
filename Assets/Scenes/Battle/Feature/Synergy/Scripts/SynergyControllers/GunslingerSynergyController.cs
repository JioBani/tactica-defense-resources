// ─────────────────────────────────────────────
// GunslingerSynergyController: 총잡이 시너지.
// 단순형 Controller. SSE 생성만 담당한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Synergy.SynergyEffects;

namespace Scenes.Battle.Feature.Synergy.SynergyControllers
{
    /// <summary>
    /// 총잡이 시너지 컨트롤러. 단순형으로 SSE 생성만 담당한다.
    /// </summary>
    public class GunslingerSynergyController : SynergyController
    {
        public GunslingerSynergyController(SynergyActivation activation) : base(activation) { }

        /// <summary>GunslingerSynergyStatusEffect 인스턴스를 생성한다.</summary>
        protected override SynergyStatusEffect CreateSynergyStatusEffect()
        {
            return new GunslingerSynergyStatusEffect(Definition.StatusEffectDefinition);
        }
    }
}
