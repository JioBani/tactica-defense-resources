namespace Scenes.Battle.Feature.Rounds.Phases
{
    public enum PhaseType
    {
        Maintenance, // 정비
        Ready, // 전투 준비
        Combat, // 전투
        End, // 전투 종료
        RoundLose, // 라운드 실패
        BattleWin, // 전투 승리
        BattleLose, // 전투 패배
    }
}