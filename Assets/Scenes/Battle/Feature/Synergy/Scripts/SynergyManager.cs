// ─────────────────────────────────────────────
// SynergyManager: 시너지 갱신 트리거, Defender 그룹핑, SynergyState 관리를 담당한다.
// 효과 적용/해제는 미구현(TODO). 소환술사 편성 시스템 구현 시 allSynergies 주입 방식을 교체한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using Common.Scripts.SerializableDictionary;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy
{
    public class SynergyManager : SceneSingleton<SynergyManager>
    {
        // TODO: 소환술사 편성 시스템 구현 후, 편성에서 결정된 시너지 목록으로 교체
        [SerializeField] private List<SynergyDefinitionData> allSynergies;
        [SerializeField] private DefenderManager defenderManager;

        private readonly Dictionary<SynergyDefinitionData, SynergyState> _synergyStates = new();

        /// <summary>모든 시너지 상태 목록. UI 표시 등 외부 조회용.</summary>
        public IReadOnlyDictionary<SynergyDefinitionData, SynergyState> SynergyStates => _synergyStates;

        // TODO: 디버그용. 시너지 UI 구현 후 제거한다.
        [Header("디버그 (런타임 확인용)")]
        [SerializeField] private SerializableDictionary<string, string> debugSynergyStatus = new();

        protected override void OnAwakeSingleton()
        {
            InitializeSynergyStates();
        }

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnDefenderPlacementChangedEventDto>(OnDefenderPlacementChanged);
            GlobalEventBus.Subscribe<OnDefenderChangedEventDto>(OnDefenderChanged);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnDefenderPlacementChangedEventDto>(OnDefenderPlacementChanged);
            GlobalEventBus.Unsubscribe<OnDefenderChangedEventDto>(OnDefenderChanged);
        }

        /// <summary>allSynergies 목록으로부터 모든 SynergyState를 미리 생성한다.</summary>
        private void InitializeSynergyStates()
        {
            foreach (var definition in allSynergies)
            {
                if (definition != null)
                    _synergyStates[definition] = new SynergyState(definition);
            }
        }

        private void OnDefenderPlacementChanged(OnDefenderPlacementChangedEventDto dto)
        {
            Recalculate();
        }

        private void OnDefenderChanged(OnDefenderChangedEventDto dto)
        {
            Recalculate();
        }

        /// <summary>전장 디펜더를 시너지별로 그룹핑하고, 각 SynergyState의 카운트·티어를 갱신한다.</summary>
        private void Recalculate()
        {
            var battleAreaDefenders = defenderManager.GetBattleAreaDefenders();
            var grouped = GroupBySynergy(battleAreaDefenders);

            foreach (var (definition, state) in _synergyStates)
            {
                int uniqueCount = CountUnique(grouped.GetValueOrDefault(definition));
                state.Recalculate(uniqueCount);
            }

            UpdateDebugStatus();
        }

        /// <summary>
        /// 디펜더를 1회 순회하며 SummonerEffect 기준으로 시너지별 그룹을 생성한다.
        /// SummonerEffect가 null이면 스킵한다.
        /// </summary>
        private Dictionary<SynergyDefinitionData, List<Defender>> GroupBySynergy(List<Defender> defenders)
        {
            var grouped = new Dictionary<SynergyDefinitionData, List<Defender>>();

            foreach (var defender in defenders)
            {
                var summonerEffect = defender.UnitLoadOutData.Unit.SummonerEffect;
                if (summonerEffect == null) continue;

                if (!grouped.TryGetValue(summonerEffect, out var list))
                {
                    list = new List<Defender>();
                    grouped[summonerEffect] = list;
                }

                list.Add(defender);
            }

            return grouped;
        }

        /// <summary>디버그용: 인스펙터에 시너지 상태를 표시한다.</summary>
        private void UpdateDebugStatus()
        {
            debugSynergyStatus.Clear();

            foreach (var (definition, state) in _synergyStates)
            {
                string tierText = state.ActiveTier.HasValue
                    ? $"Tier {state.ActiveTier.Value.Tier}"
                    : "비활성";
                debugSynergyStatus[definition.DisplayName] = $"{state.Count}명 → {tierText}";
            }
        }

        /// <summary>
        /// UnitDefinitionData.ID 기준으로 중복을 제거한 유니크 유닛 수를 반환한다.
        /// </summary>
        private int CountUnique(List<Defender> defenders)
        {
            if (defenders == null || defenders.Count == 0) return 0;

            var uniqueIds = new HashSet<int>();

            foreach (var defender in defenders)
            {
                uniqueIds.Add(defender.UnitLoadOutData.Unit.ID);
            }

            return uniqueIds.Count;
        }
    }
}