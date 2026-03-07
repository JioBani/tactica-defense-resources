using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;

namespace Scenes.Battle.Feature.Events
{
    /// <summary>시너지 티어가 변경(활성화/변경/비활성화)되었을 때 발행되는 이벤트.</summary>
    public struct OnSynergyTierChangedEventDto : IGameEvent
    {
        /// <summary>변경된 시너지 정의.</summary>
        public SynergyDefinitionData Definition;

        /// <summary>변경 전 티어. 이전에 비활성이었으면 null.</summary>
        public SynergyTier? PreviousTier;

        /// <summary>변경 후 티어. 비활성화되었으면 null.</summary>
        public SynergyTier? CurrentTier;

        public OnSynergyTierChangedEventDto(
            SynergyDefinitionData definition,
            SynergyTier? previousTier,
            SynergyTier? currentTier)
        {
            Definition = definition;
            PreviousTier = previousTier;
            CurrentTier = currentTier;
        }
    }
}