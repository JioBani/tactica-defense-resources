using Common.Data.Synergies;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Units;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// 시너지 SE Apply 시 전달되는 Context.
    /// SynergyActivation, SynergyDefinitionData, 대상 Unit을 SSE에 주입한다.
    /// </summary>
    public class SynergyStatusEffectContext : StatusEffectContext
    {
        /// <summary>이 시너지의 카운트·티어 상태. ActiveTier 구독용.</summary>
        public SynergyActivation Activation { get; }

        /// <summary>이 시너지의 정의 데이터.</summary>
        public SynergyDefinitionData Definition { get; }

        /// <summary>SSE가 적용된 대상 유닛. StatSheet 접근 등에 사용.</summary>
        public Units.Unit Unit { get; }

        public SynergyStatusEffectContext(
            SynergyActivation activation,
            SynergyDefinitionData definition,
            Units.Unit unit)
        {
            Activation = activation;
            Definition = definition;
            Unit = unit;
        }
    }
}
