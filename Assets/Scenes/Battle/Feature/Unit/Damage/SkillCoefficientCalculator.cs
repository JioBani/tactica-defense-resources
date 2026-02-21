using Common.Data.Skills;
using Scenes.Battle.Feature.Units.UnitStats.UnitStatSheets;

namespace Scenes.Battle.Feature.Units.Damage
{
    /// <summary>
    /// SkillCoefficient의 최종값을 산출하는 유틸리티.
    /// 최종값 = baseValue + Σ(statSheet.Get(scaling.statKind).CurrentValue × scaling.coefficient)
    /// </summary>
    public static class SkillCoefficientCalculator
    {
        /// <summary>
        /// SkillCoefficient와 스탯 시트를 받아 최종 계수값을 계산한다.
        /// </summary>
        public static float Calculate(SkillCoefficient coeff, UnitStatSheet statSheet)
        {
            float total = coeff.baseValue;

            if (coeff.scalings == null) return total;

            for (int i = 0; i < coeff.scalings.Length; i++)
            {
                var stat = statSheet.Get(coeff.scalings[i].statKind);
                if (stat != null)
                    total += stat.CurrentValue * coeff.scalings[i].coefficient;
            }

            return total;
        }
    }
}
