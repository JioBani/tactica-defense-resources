namespace Scenes.Battle.Feature.Units.ActionStates
{
    public enum ActionStateType
    {
        Idle,
        Move,
        Attack,
        Downed,
        /// <summary>전투 종료 후 결과 표시 중 정지 상태. 다운 외형/HP바를 유지한다.</summary>
        Freeze,
        /// <summary>Maintenance 중 대기 상태. 무적이며 HP 회복 후 다음 전투를 준비한다.</summary>
        Waiting,
    }
}