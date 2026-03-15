using Common.Data.Synergies;
using Common.Scripts.StatusEffect;
using Scenes.Battle.Feature.Units;

namespace Scenes.Battle.Feature.Synergy
{
    /// <summary>
    /// TierLinkedStatusEffect에 전달되는 Context.
    /// SynergyActivation을 통해 ActiveTier를 구독하고, 대상 Unit의 스탯에 접근한다.
    /// </summary>
    public class TierLinkedStatusEffectContext : StatusEffectContext
    {
        /// <summary>이 시너지의 카운트·티어 상태. ActiveTier 구독용.</summary>
        public SynergyActivation Activation { get; }

        /// <summary>이 시너지의 정의 데이터. 티어별 상수 접근용.</summary>
        public SynergyDefinitionData Definition { get; }

        /// <summary>SE가 적용된 대상 유닛. StatSheet 접근 등에 사용.</summary>
        public Units.Unit Unit { get; }

        public TierLinkedStatusEffectContext(
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