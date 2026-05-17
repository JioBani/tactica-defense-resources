using Common.Scripts.GlobalEventBus;

namespace Scenes.Battle.Feature.Events.RoundEvents
{
    /// <summary>SummonTrait 분배가 완료되어 SummonTraitStore에 보관되었음을 알리는 이벤트.</summary>
    public struct OnSummonTraitsDistributedEventDto : IGameEvent
    {
    }
}
