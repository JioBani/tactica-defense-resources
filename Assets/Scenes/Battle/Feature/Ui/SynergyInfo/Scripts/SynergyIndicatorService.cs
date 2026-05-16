using System;
using System.Collections.Generic;
using Common.Data.Synergies;

namespace Scenes.Battle.Feature.Ui.SynergyInfo
{
    /// <summary>
    /// SynergyIndicator의 비율 계산 로직을 담당하는 서비스 클래스.
    /// MonoBehaviour에서 분리하여 유닛 테스트가 가능하도록 한다.
    /// </summary>
    public class SynergyIndicatorService
    {
        /// <summary>각 티어 도트의 inner 너비 비율을 현재 카운트에 따라 계산한다.</summary>
        /// <param name="tiers">시너지 티어 목록 (requiredCount 오름차순)</param>
        /// <param name="count">현재 유니크 유닛 카운트</param>
        /// <param name="dotCount">표시할 도트 수</param>
        /// <returns>각 도트의 비율 배열 (0.0 ~ 1.0)</returns>
        public float[] CalculateRatios(IReadOnlyList<SynergyTier> tiers, int count, int dotCount)
        {
            int effectiveDotCount = Math.Min(tiers.Count, dotCount);
            float[] ratios = new float[effectiveDotCount];

            for (int i = 0; i < effectiveDotCount; i++)
            {
                int rangeStart = i == 0 ? 0 : tiers[i - 1].RequiredCount;
                int rangeEnd = tiers[i].RequiredCount;

                float ratio;
                if (count >= rangeEnd)
                {
                    ratio = 1f;
                }
                else if (count <= rangeStart)
                {
                    ratio = 0f;
                }
                else
                {
                    ratio = (float)(count - rangeStart) / (rangeEnd - rangeStart);
                }

                ratios[i] = ratio;
            }

            return ratios;
        }
    }
}