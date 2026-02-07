namespace Scenes.Battle.Feature.Rounds.Phases
{
    public enum PhaseType
    {
        Maintenance, // 정비
        Ready, // 전투 준비
        Combat, // 전투
        End, // 전투 종료
        GameWin,
        GameOver,
    }
}