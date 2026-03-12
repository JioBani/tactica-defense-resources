using Common.Data.Synergies;
using Common.Scripts.StatusEffect;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지 SE Apply 시 전달되는 Context.
    /// SynergyActivation과 SynergyDefinitionData를 SSE에 주입한다.
    /// </summary>
    public class SynergyStatusEffectContext : StatusEffectContext
    {
        /// <summary>이 시너지의 카운트·티어 상태. ActiveTier 구독용.</summary>
        public SynergyActivation Activation { get; }

        /// <summary>이 시너지의 정의 데이터.</summary>
        public SynergyDefinitionData Definition { get; }

        public SynergyStatusEffectContext(SynergyActivation activation, SynergyDefinitionData definition)
        {
            Activation = activation;
            Definition = definition;
        }
    }
}
