// ─────────────────────────────────────────────
// WarmongerSynergyController: 전쟁기계 시너지.
// DefenderManager를 주입받아 SSE 생성 시 전달한다.
// ─────────────────────────────────────────────
using Scenes.Battle.Feature.Synergy.SynergyEffects;
using Scenes.Battle.Feature.Unit.Defenders;

namespace Scenes.Battle.Feature.Synergy.SynergyControllers
{
    /// <summary>
    /// 전쟁기계 시너지 컨트롤러. DefenderManager를 SSE에 주입하여
    /// 사망 시 다른 전쟁기계 회복이 가능하게 한다.
    /// </summary>
    public class WarmongerSynergyController : SynergyController
    {
        private readonly DefenderManager _defenderManager;

        public WarmongerSynergyController(SynergyActivation activation, DefenderManager defenderManager)
            : base(activation)
        {
            _defenderManager = defenderManager;
        }

        /// <summary>WarmongerSynergyStatusEffect 인스턴스를 생성한다.</summary>
        protected override SynergyStatusEffect CreateSynergyStatusEffect()
        {
            return new WarmongerSynergyStatusEffect(Definition.StatusEffectDefinition, _defenderManager);
        }
    }
}
