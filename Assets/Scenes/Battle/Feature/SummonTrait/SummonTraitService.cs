using System.Collections.Generic;
using Common.Data.Summoners.SummonerLoadOuts;
using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using UnityEngine;

namespace Scenes.Battle.Feature.SummonTrait
{
    /// <summary>소환수 풀 전체에 특성을 균등 랜덤으로 분배한다.</summary>
    public class SummonTraitService
    {
        /// <summary>소환술사 목록과 특성 풀을 받아 소환수별 균등 랜덤 배정 결과를 반환한다.</summary>
        public Dictionary<UnitLoadOutData, SynergyDefinitionData> Distribute(
            IReadOnlyList<SummonerLoadOutData> summoners,
            IReadOnlyList<SynergyDefinitionData> traitPool)
        {
            if (traitPool == null || traitPool.Count == 0)
            {
                Debug.LogError("[SummonTraitService] 특성 풀이 비어 있어 분배 불가");
                return new Dictionary<UnitLoadOutData, SynergyDefinitionData>();
            }

            var result = new Dictionary<UnitLoadOutData, SynergyDefinitionData>();

            foreach (var summoner in summoners)
            {
                if (summoner == null)
                {
                    continue;
                }

                var summonPool = summoner.Summoner?.SummonPool;

                if (summonPool == null || summonPool.Length == 0)
                {
                    Debug.LogWarning($"[SummonTraitService] SummonPool이 비어있는 소환술사 발견: {summoner.name}");
                    continue;
                }

                foreach (var unit in summonPool)
                {
                    if (unit == null)
                    {
                        Debug.LogWarning("[SummonTraitService] null UnitLoadOutData 발견, 스킵");
                        continue;
                    }

                    result[unit] = traitPool[Random.Range(0, traitPool.Count)];
                }
            }

            return result;
        }
    }
}
