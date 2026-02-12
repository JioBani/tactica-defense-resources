using UnityEngine;

namespace Common.Data.Configs
{
    [CreateAssetMenu(menuName = "GameConfig/EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("기본 수입")]
        [Tooltip("인덱스 = 라운드 번호, 값 = 해당 라운드 기본 지급 마나")]
        public int[] baseManaPerRound;

        [Header("이자 관련")]
        public int manaPerInterestStep = 10;   // 10마나당 이자
        public int interestPerStep = 1;        // 10마나당 1마나
        public int maxInterest = 3;            // 최대 3마나

        [Header("연승/연패 보너스")]
        public int[] winStreakBonusByCount;    // 인덱스 = 연승길이
        public int[] loseStreakBonusByCount;   // 인덱스 = 연패길이

        public int GetBaseMana(int roundIndex)
        {
            if (baseManaPerRound == null || baseManaPerRound.Length == 0)
                return 0;

            int index = Mathf.Clamp(roundIndex, 0, baseManaPerRound.Length - 1);
            return baseManaPerRound[index];
        }

        public int GetRoundStartIncome(int roundIndex, int currentMana, int winStreak, int loseStreak)
        {
            int income = GetBaseMana(roundIndex);

            // 이자
            int steps = Mathf.Min(currentMana / manaPerInterestStep, maxInterest);
            income += steps * interestPerStep;

            // 연승 보너스
            if (winStreak > 0 && winStreak < winStreakBonusByCount.Length)
                income += winStreakBonusByCount[winStreak];

            // 연패 보너스
            if (loseStreak > 0 && loseStreak < loseStreakBonusByCount.Length)
                income += loseStreakBonusByCount[loseStreak];

            return income;
        }
    }
}

