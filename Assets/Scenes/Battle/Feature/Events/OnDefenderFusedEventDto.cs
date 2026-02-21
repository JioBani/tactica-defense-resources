using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Unit.Defenders;

namespace Scenes.Battle.Feature.Events
{
    /// <summary>소환수 합성(승급) 완료 시 발행되는 이벤트.</summary>
    public struct OnDefenderFusedEventDto : IGameEvent
    {
        /// <summary>승급된 디펜더</summary>
        public Defender Survivor;

        /// <summary>승급 후 성급</summary>
        public int NewStar;

        public OnDefenderFusedEventDto(Defender survivor, int newStar)
        {
            Survivor = survivor;
            NewStar = newStar;
        }
    }
}
