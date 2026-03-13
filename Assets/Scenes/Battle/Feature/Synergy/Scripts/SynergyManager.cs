// ─────────────────────────────────────────────
// SynergyManager: 시너지 갱신 트리거, Defender 그룹핑, SynergyActivation 관리를 담당한다.
// 티어 활성화 시 대상 Defender의 StatusEffectController에 SSE를 직접 Apply한다.
// 소환술사 편성 시스템 구현 시 allSynergies 주입 방식을 교체한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using Common.Scripts.SerializableDictionary;
using Common.Scripts.StatusEffect;
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

        private readonly Dictionary<SynergyDefinitionData, SynergyActivation> _synergyActivations = new();
        private readonly SynergyStatusEffectFactory _synergyStatusEffectFactory = new();

        /// <summary>모든 시너지 상태 목록. UI 표시 등 외부 조회용.</summary>
        public IReadOnlyDictionary<SynergyDefinitionData, SynergyActivation> SynergyActivations => _synergyActivations;

        // TODO: 디버그용. 시너지 UI 구현 후 제거한다.
        [Header("디버그 (런타임 확인용)")]
        [SerializeField] private SerializableDictionary<string, string> debugSynergyStatus = new();

        protected override void OnAwakeSingleton()
        {
            InitializeSynergyActivations();
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

        /// <summary>allSynergies 목록으로부터 모든 SynergyActivation를 미리 생성한다.</summary>
        private void InitializeSynergyActivations()
        {
            foreach (SynergyDefinitionData definition in allSynergies)
            {
                if (definition != null)
                    _synergyActivations[definition] = new SynergyActivation(definition);
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

        /// <summary>전장 디펜더를 시너지별로 그룹핑하고, 각 SynergyActivation의 카운트·티어를 갱신한다.</summary>
        private void Recalculate()
        {
            List<Defender> battleAreaDefenders = defenderManager.GetBattleAreaDefenders();
            Dictionary<SynergyDefinitionData, List<Defender>> grouped = GroupBySynergy(battleAreaDefenders);

            foreach ((SynergyDefinitionData definition, SynergyActivation activation) in _synergyActivations)
            {
                int uniqueCount = CountUnique(grouped.GetValueOrDefault(definition));
                TierChangeResult result = activation.Recalculate(uniqueCount);

                if (result.Changed)
                {
                    // 비활성→활성 전환 시 대상 Defender에 SSE Apply
                    if (!result.PreviousTier.HasValue && activation.ActiveTier.Value.HasValue)
                        ApplySynergyEffects(definition, activation, grouped.GetValueOrDefault(definition));
                }
            }

            UpdateDebugStatus();
        }

        /// <summary>
        /// 시너지가 활성화될 때, 해당 시너지를 보유한 Defender들에게 SSE를 Apply한다.
        /// </summary>
        private void ApplySynergyEffects(
            SynergyDefinitionData definition,
            SynergyActivation activation,
            List<Defender> defenders)
        {
            if (defenders == null) return;

            foreach (Defender defender in defenders)
            {
                StatusEffectController controller = defender.StatusEffectController;
                if (controller == null)
                    throw new MissingComponentException(
                        $"{defender.name}에 StatusEffectController가 없습니다.");

                SynergyStatusEffect effect = _synergyStatusEffectFactory.Create(definition.Id);
                var context = new SynergyStatusEffectContext(activation, definition, defender);
                controller.Apply(effect, context);
            }
        }

        /// <summary>
        /// 디펜더를 1회 순회하며 보유 시너지별 그룹을 생성한다.
        /// 시너지 종류(소환술사 효과, 소환수 특성 등)를 구분하지 않는다.
        /// </summary>
        private Dictionary<SynergyDefinitionData, List<Defender>> GroupBySynergy(List<Defender> defenders)
        {
            Dictionary<SynergyDefinitionData, List<Defender>> grouped = new Dictionary<SynergyDefinitionData, List<Defender>>();

            foreach (Defender defender in defenders)
            {
                IReadOnlyList<SynergyDefinitionData> synergies = defender.UnitLoadOutData.Unit.Synergies;

                foreach (SynergyDefinitionData synergy in synergies)
                {
                    if (!grouped.TryGetValue(synergy, out List<Defender> list))
                    {
                        list = new List<Defender>();
                        grouped[synergy] = list;
                    }

                    list.Add(defender);
                }
            }

            return grouped;
        }

        /// <summary>디버그용: 인스펙터에 시너지 상태를 표시한다.</summary>
        private void UpdateDebugStatus()
        {
            debugSynergyStatus.Clear();

            foreach ((SynergyDefinitionData definition, SynergyActivation activation) in _synergyActivations)
            {
                string tierText = activation.ActiveTier.Value.HasValue
                    ? $"Tier {activation.ActiveTier.Value.Value.Tier}"
                    : "비활성";
                debugSynergyStatus[definition.DisplayName] = $"{activation.Count}명 → {tierText}";
            }
        }

        /// <summary>
        /// UnitDefinitionData.ID 기준으로 중복을 제거한 유니크 유닛 수를 반환한다.
        /// </summary>
        private int CountUnique(List<Defender> defenders)
        {
            if (defenders == null || defenders.Count == 0) return 0;

            HashSet<int> uniqueIds = new HashSet<int>();

            foreach (Defender defender in defenders)
            {
                uniqueIds.Add(defender.UnitLoadOutData.Unit.ID);
            }

            return uniqueIds.Count;
        }
    }
}