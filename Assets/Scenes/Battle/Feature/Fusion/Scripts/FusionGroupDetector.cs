// ─────────────────────────────────────────────
// FusionGroupDetector: 동일 소환수 합성 그룹을 탐지하는 순수 로직 클래스.
// Unity 의존 없이 테스트 가능하다. DefenderFusionManager에서 사용한다.
// 그룹 탐지만 담당하며, 승급 대상 선정(위치 기반)은 Manager에서 처리한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using System.Linq;

namespace Scenes.Battle.Feature.Fusion
{
    /// <summary>합성 후보 데이터. Defender에서 추출하여 전달한다.</summary>
    public readonly struct FusionCandidate
    {
        /// <summary>UnitDefinitionData.ID</summary>
        public readonly int UnitDefinitionId;

        /// <summary>현재 성급</summary>
        public readonly int Star;

        /// <summary>Defenders 리스트 내 인덱스</summary>
        public readonly int Index;

        public FusionCandidate(int unitDefinitionId, int star, int index)
        {
            UnitDefinitionId = unitDefinitionId;
            Star = star;
            Index = index;
        }
    }

    /// <summary>
    /// 동일 (UnitDefinitionId, Star) 그룹에서 3개 이상이면 합성 그룹으로 판정한다.
    /// 합성 가능한 그룹의 인덱스 배열을 반환한다.
    /// </summary>
    public class FusionGroupDetector
    {
        /// <summary>
        /// 합성 가능한 그룹을 하나 찾아 인덱스 배열로 반환한다. 없으면 null.
        /// maxStar 미만의 성급에서만 기본 합성을 허용한다. (예: maxStar=3이면 1성, 2성만 합성)
        /// </summary>
        public int[] FindFusionGroup(IReadOnlyList<FusionCandidate> candidates, int maxStar = int.MaxValue)
        {
            if (candidates == null || candidates.Count < 3) return null;

            // (UnitDefinitionId, Star) 기준으로 그룹핑, maxStar 미만만 허용
            var group = candidates
                .GroupBy(c => (c.UnitDefinitionId, c.Star))
                .FirstOrDefault(g => g.Key.Star < maxStar && g.Count() >= 3);

            if (group == null) return null;

                return group.Select(c => c.Index).ToArray();
            }

            /// <summary>
            /// 합성 가능한 그룹이 존재하는지 확인한다.
            /// </summary>
            public bool HasFusionGroup(IReadOnlyList<FusionCandidate> candidates, int maxStar = int.MaxValue)
            {
                return FindFusionGroup(candidates, maxStar) != null;
            }
    }
}
