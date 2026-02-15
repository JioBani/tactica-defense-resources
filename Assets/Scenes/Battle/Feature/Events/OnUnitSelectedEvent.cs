using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Units;

namespace Scenes.Battle.Feature.Events
{
    /// <summary>
    /// 유닛 선택/해제 이벤트.
    /// Unit이 null이면 선택 해제를 의미한다.
    /// </summary>
    public struct OnUnitSelectedEvent : IGameEvent
    {
        public readonly Units.Unit Unit;

        public OnUnitSelectedEvent(Units.Unit unit)
        {
            Unit = unit;
        }
    }
}
