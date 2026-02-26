using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace Common.Data.Configs
{
    [Serializable]
    public struct StarProbabilityEntry
    {
        [Tooltip("배치 상한 레벨")]
        public int level;

        [Tooltip("2성 등장 확률 (0~1)")]
        [Range(0f, 1f)]
        public float twoStarRate;

        [Tooltip("3성 등장 확률 (0~1)")]
        [Range(0f, 1f)]
        public float threeStarRate;
    }

    /// <summary>
    /// 배치 상한 레벨별 스캔 시 소환수 성급 등장 확률을 정의한다.
    /// 1성 확률은 (1 - twoStarRate - threeStarRate)로 암묵 결정된다.
    /// </summary>
    [CreateAssetMenu(menuName = "GameConfig/StarProbabilityConfig")]
    public class StarProbabilityConfig : ScriptableObject
    {
        [Tooltip("레벨별 성급 등장 확률. level 오름차순으로 정렬한다.")]
        public StarProbabilityEntry[] entries;

        /// <summary>
        /// 주어진 배치 상한 레벨에 해당하는 성급을 확률적으로 결정한다.
        /// entries에서 level 이하인 항목 중 가장 높은 level의 entry를 사용한다.
        /// 해당하는 entry가 없으면 1성을 반환한다.
        /// </summary>
        public int PickStar(int level)
        {
            StarProbabilityEntry? matched = FindEntry(level);

            if (!matched.HasValue)
                return 1;

            var entry = matched.Value;
            float roll = Random.value;

            if (roll < entry.threeStarRate)
                return 3;

            if (roll < entry.threeStarRate + entry.twoStarRate)
                return 2;

            return 1;
        }

        /// <summary>
        /// 주어진 레벨 이하에서 가장 가까운 entry를 찾는다.
        /// </summary>
        private StarProbabilityEntry? FindEntry(int level)
        {
            StarProbabilityEntry? best = null;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].level <= level)
                {
                    if (!best.HasValue || entries[i].level > best.Value.level)
                        best = entries[i];
                }
            }

            return best;
        }
    }
}
