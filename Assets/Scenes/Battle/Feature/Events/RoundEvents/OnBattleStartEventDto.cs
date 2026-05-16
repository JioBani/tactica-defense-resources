using Common.Scripts.GlobalEventBus;

namespace Scenes.Battle.Feature.Events.RoundEvents
{
    /// <summary>전장 시작 시 발행되는 이벤트. RoundManager.Start()에서 1회 발행된다.</summary>
    public struct OnBattleStartEventDto : IGameEvent
    {
    }
}
