// ─────────────────────────────────────────────
// EnhancementFusionDetector: 강화 합성 쌍을 탐지하는 순수 로직 클래스.
// 3성 이상 타겟과 동일 종류 2성 또는 3성 재료 쌍을 찾는다.
// 2성 재료는 +1 강화, 3성 재료는 +2 강화를 부여한다.
// Unity 의존 없이 테스트 가능하다. DefenderFusionManager에서 사용한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;

namespace Scenes.Battle.Feature.Fusion
{
    /// <summary>강화 합성 탐지 결과. 타겟과 재료의 Defenders 리스트 내 인덱스를 담는다.</summary>
    public readonly struct EnhancementFusionResult
    {
        /// <summary>강화 대상 (3성+) 디펜더의 인덱스</summary>
        public readonly int TargetIndex;

        /// <summary>재료 (2성 또는 3성) 디펜더의 인덱스</summary>
        public readonly int MaterialIndex;

        public EnhancementFusionResult(int targetIndex, int materialIndex)
        {
            TargetIndex = targetIndex;
            MaterialIndex = materialIndex;
        }
    }

    /// <summary>
    /// 동일 종류 3성+ 유닛과 2성/3성 유닛 쌍을 찾아 강화 합성 후보로 반환한다.
    /// </summary>
    public class EnhancementFusionDetector
    {
        private const int MinTargetStar = 3;

        /// <summary>
        /// 해당 성급이 강화 재료로 사용 가능한지 확인한다.
        /// </summary>
        public bool IsMaterialStar(int star) => star == 2 || star == 3;

        /// <summary>
        /// 강화 합성 가능한 쌍을 하나 찾아 반환한다. 없으면 null.
        /// </summary>
        public EnhancementFusionResult? FindEnhancementPair(IReadOnlyList<FusionCandidate> candidates)
        {
            if (candidates == null || candidates.Count < 2) return null;

            for (int t = 0; t < candidates.Count; t++)
            {
                var target = candidates[t];
                if (target.Star < MinTargetStar) continue;

                for (int m = 0; m < candidates.Count; m++)
                {
                    if (m == t) continue;

                    var material = candidates[m];
                    if (!IsMaterialStar(material.Star)) continue;
                    if (material.UnitDefinitionId != target.UnitDefinitionId) continue;

                    return new EnhancementFusionResult(target.Index, material.Index);
                }
            }

            return null;
        }

        /// <summary>
        /// 강화 합성 가능한 쌍이 존재하는지 확인한다.
        /// </summary>
        public bool HasEnhancementPair(IReadOnlyList<FusionCandidate> candidates)
        {
            return FindEnhancementPair(candidates) != null;
        }
    }
}
