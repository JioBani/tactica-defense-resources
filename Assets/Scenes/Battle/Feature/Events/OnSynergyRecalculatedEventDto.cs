using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Synergy;

namespace Scenes.Battle.Feature.Events
{
    /// <summary>시너지 재계산 완료 시 발행된다.</summary>
    public struct OnSynergyRecalculatedEventDto : IGameEvent
    {
        /// <summary>재계산된 시너지 활성화 상태.</summary>
        public SynergyActivation Activation { get; private set; }

        public OnSynergyRecalculatedEventDto(SynergyActivation activation)
        {
            Activation = activation;
        }
    }
}
