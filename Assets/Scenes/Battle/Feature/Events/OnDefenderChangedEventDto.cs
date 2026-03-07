using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Unit.Defenders;

namespace Scenes.Battle.Feature.Events
{
    public struct OnDefenderChangedEventDto : IGameEvent
    {
        public Defender Defender { get; private set; }
        public DefenderChanges Change { get; private set; }

        public OnDefenderChangedEventDto(Defender defender, DefenderChanges change)
        {
            Defender = defender;
            Change = change;
        }
    }
}