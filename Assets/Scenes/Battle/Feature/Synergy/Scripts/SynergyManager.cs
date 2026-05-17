// ─────────────────────────────────────────────
// SynergyManager: SynergyController 생성과 Dispose, 디버그 표시를 담당하는 얇은 레이어.
// 비즈니스 로직은 SynergyController에서 일원 관리한다.
// Start()에서 SummonerManager의 편성 데이터를 읽어 시너지 시스템을 자체 초기화한다.
// ─────────────────────────────────────────────
using System;
using System.Collections.Generic;
using Common.Data.Summoners.SummonerLoadOuts;
using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneSingleton;
using Common.Scripts.SerializableDictionary;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.SummonTrait;
using Scenes.Battle.Feature.Unit.Defenders;
using Scenes.Battle.Feature.Unit.Summoners;
using UnityEngine;

namespace Scenes.Battle.Feature.Synergy
{
    public class SynergyManager : SceneSingleton<SynergyManager>
    {
        private readonly Dictionary<SynergyDefinitionData, SynergyActivation> _synergyActivations = new();
        private readonly Dictionary<SynergyDefinitionData, SynergyController> _controllers = new();

        /// <summary>소환수 → 소환술사 효과 역방향 매핑.</summary>
        private readonly Dictionary<UnitLoadOutData, SynergyDefinitionData> _unitSynergyMap = new();

        /// <summary>소환수 → SummonTrait 시너지 역방향 매핑. OnSummonTraitsDistributedEventDto 수신 시 채워진다.</summary>
        private readonly Dictionary<UnitLoadOutData, SynergyDefinitionData> _unitSummonTraitMap = new();

        /// <summary>시너지 → 보유 소환수 역참조 인덱스 (내부).</summary>
        private readonly Dictionary<SynergyDefinitionData, List<UnitLoadOutData>> _synergyMembers = new();

        /// <summary>외부 노출용 readonly 뷰 — _synergyMembers 의 값 컬렉션을 IReadOnlyList 로 노출.</summary>
        private Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>> _synergyMembersPublic = new();

        /// <summary>모든 시너지 상태 목록. UI 표시 등 외부 조회용.</summary>
        public IReadOnlyDictionary<SynergyDefinitionData, SynergyActivation> SynergyActivations => _synergyActivations;

        /// <summary>
        /// 시너지 → 보유 소환수 목록 역참조 통로. UI(시너지 상세 패널 등) 외부 조회용.
        /// 키 부재 시 사용자 측이 빈 목록으로 해석. 본 인덱스는 Start() 시점에 한 번 구축되며 이후 변경되지 않는다.
        /// 시너지 종류(소환술사 효과 / 소환수 특성) 무관 일반 책임 — 현재는 소환술사 효과 시너지에 한해 누적되며,
        /// 향후 소환수 특성 분배 도메인 로직이 추가되면 동일 인덱스에 자연 합산된다.
        /// </summary>
        public IReadOnlyDictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>> SynergyMembers => _synergyMembersPublic;

        // TODO: 디버그용. 시너지 UI 구현 후 제거한다.
        [Header("디버그 (런타임 확인용)")]
        [SerializeField] private SerializableDictionary<string, string> debugSynergyStatus = new();

        protected override void OnAwakeSingleton() { }

        private void Start()
        {
            IReadOnlyList<SummonerLoadOutData> summoners = SummonerManager.Instance.Summoners;

            // 소환수 → 시너지 역방향 맵 구축 (Defender 스폰 시 시너지 주입에 사용)
            BuildUnitSynergyMap(summoners);

            // 시너지 → 보유 소환수 역참조 인덱스 구축 (UI 외부 조회용)
            BuildSynergyMembersMap(summoners);

            // 유니크 소환술사 효과 추출 → SynergyController 생성
            var uniqueEffects = new HashSet<SynergyDefinitionData>();
            foreach (SummonerLoadOutData summoner in summoners)
            {
                uniqueEffects.Add(summoner.Summoner.SummonerEffect);
            }

            InitializeSynergyActivations(uniqueEffects);

            // SummonTrait 분배 완료 이벤트 구독 (SummonerEffect 초기화 이후로 위치).
            // OQ-9: Unity Start() 객체 간 순서가 미보장이므로 구독 직후 SummonTraitStore 상태를 동기 검사하여
            // 이벤트가 본 메서드 진입 전에 이미 발행된 케이스의 누락을 보정한다.
            GlobalEventBus.Subscribe<OnSummonTraitsDistributedEventDto>(HandleSummonTraitsDistributed);

            if (SummonTraitStore.Instance != null && SummonTraitStore.Instance.All.Count > 0)
            {
                HandleSummonTraitsDistributed(new OnSummonTraitsDistributedEventDto());
            }
        }

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
            GlobalEventBus.Unsubscribe<OnSummonTraitsDistributedEventDto>(HandleSummonTraitsDistributed);

            foreach (SynergyController controller in _controllers.Values)
            {
                controller.Dispose();
            }
        }

        // ── 초기화 ──

        /// <summary>Formation의 SummonPool을 순회하여 역방향 맵을 구축한다.</summary>
        private void BuildUnitSynergyMap(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            _unitSynergyMap.Clear();

            foreach (SummonerLoadOutData summoner in summoners)
            {
                SynergyDefinitionData effect = summoner.Summoner.SummonerEffect;
                if (effect == null)
                {
                    throw new InvalidOperationException(
                        $"[SynergyManager] {summoner.name}의 SummonerEffect가 null입니다.");
                }

                foreach (UnitLoadOutData unit in summoner.Summoner.SummonPool)
                {
                    if (unit == null)
                    {
                        throw new InvalidOperationException(
                            $"[SynergyManager] {summoner.name}의 SummonPool에 null 항목이 있습니다.");
                    }

                    _unitSynergyMap[unit] = effect;
                }
            }
        }

        /// <summary>
        /// 시너지 → 보유 소환수 목록 역참조 인덱스를 구축한다.
        /// 각 소환술사 로드아웃의 SummonerEffect 시너지 아래에 그 소환술사의 SummonPool 소환수를 누적하며,
        /// 동일 소환수가 여러 풀에 등장하더라도 중복은 자연스럽게 제거된다.
        /// 시너지 종류 분기 없음 — SummonerEffect 가 본 이슈 시점의 표시 활성화 범위(소환술사 효과 시너지)를
        /// 자연 표현하며, 향후 소환수 특성 분배 도메인 로직이 추가되어 동일 인덱스에 누적되면 자동 합산된다.
        /// </summary>
        private void BuildSynergyMembersMap(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            _synergyMembers.Clear();

            foreach (SummonerLoadOutData summoner in summoners)
            {
                SynergyDefinitionData effect = summoner.Summoner.SummonerEffect;
                if (effect == null)
                {
                    continue;
                }

                if (!_synergyMembers.TryGetValue(effect, out List<UnitLoadOutData> list))
                {
                    list = new List<UnitLoadOutData>();
                    _synergyMembers[effect] = list;
                }

                foreach (UnitLoadOutData unit in summoner.Summoner.SummonPool)
                {
                    if (unit != null && !list.Contains(unit))
                    {
                        list.Add(unit);
                    }
                }
            }

            // 외부 노출 readonly 뷰 재구성
            _synergyMembersPublic = new Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>>(_synergyMembers.Count);
            foreach (KeyValuePair<SynergyDefinitionData, List<UnitLoadOutData>> kv in _synergyMembers)
            {
                _synergyMembersPublic[kv.Key] = kv.Value;
            }
        }

        /// <summary>시너지 목록으로부터 SynergyActivation과 SynergyController를 생성한다.</summary>
        private void InitializeSynergyActivations(HashSet<SynergyDefinitionData> synergies)
        {
            foreach (SynergyDefinitionData definition in synergies)
            {
                var activation = new SynergyActivation(definition);
                _synergyActivations[definition] = activation;
                _controllers[definition] = SynergyControllerFactory.Instance.Create(activation);

                activation.ActiveTier.OnChange += _ => UpdateDebugStatus();
            }
        }

        // ── Defender Spawn 처리 ──

        /// <summary>Defender 스폰 시 역방향 맵에서 소환술사 효과와 SummonTrait를 조회하여 둘 다 주입한다.</summary>
        private void HandleDefenderChanged(OnDefenderChangedEventDto dto)
        {
            if (dto.Change == DefenderChanges.Spawn)
            {
                UnitLoadOutData unit = dto.Defender.UnitLoadOutData;

                if (_unitSynergyMap.TryGetValue(unit, out SynergyDefinitionData summonerEffect))
                {
                    dto.Defender.AddSynergy(summonerEffect);
                }

                if (_unitSummonTraitMap.TryGetValue(unit, out SynergyDefinitionData summonTrait))
                {
                    dto.Defender.AddSynergy(summonTrait);
                }
            }
        }

        /// <summary>
        /// SummonTrait 분배 완료 시 SummonTraitStore에서 결과를 읽어 활성화·역참조 인덱스에 합산하고
        /// 각 SummonTrait별 SynergyController를 생성한다.
        /// </summary>
        private void HandleSummonTraitsDistributed(OnSummonTraitsDistributedEventDto _)
        {
            if (SummonTraitStore.Instance == null)
            {
                Debug.LogError("[SynergyManager] SummonTraitStore 미존재 — SummonTrait 통합 스킵");
                return;
            }

            SerializableDictionary<UnitLoadOutData, SynergyDefinitionData> traitMap = SummonTraitStore.Instance.All;

            // SummonTrait 역방향 맵 구축 (Defender 스폰 시 시너지 주입에 사용)
            _unitSummonTraitMap.Clear();
            foreach (KeyValuePair<UnitLoadOutData, SynergyDefinitionData> kv in traitMap)
            {
                _unitSummonTraitMap[kv.Key] = kv.Value;
            }

            // 시너지 → 보유 소환수 목록 재구성 (SummonTrait 역참조 인덱스)
            var traitToUnits = new Dictionary<SynergyDefinitionData, List<UnitLoadOutData>>();
            foreach (KeyValuePair<UnitLoadOutData, SynergyDefinitionData> kv in traitMap)
            {
                if (!traitToUnits.TryGetValue(kv.Value, out List<UnitLoadOutData> list))
                {
                    list = new List<UnitLoadOutData>();
                    traitToUnits[kv.Value] = list;
                }
                list.Add(kv.Key);
            }

            // 각 SummonTrait에 대해 활성화·컨트롤러·역참조 인덱스를 등록
            foreach (KeyValuePair<SynergyDefinitionData, List<UnitLoadOutData>> kv in traitToUnits)
            {
                SynergyDefinitionData trait = kv.Key;
                List<UnitLoadOutData> units = kv.Value;

                if (_synergyActivations.ContainsKey(trait))
                {
                    Debug.LogWarning($"[SynergyManager] SummonTrait '{trait.DisplayName}'이 이미 등록됨 — 기존 항목 유지 (전장 1회 분배 전제)");
                    continue;
                }

                var activation = new SynergyActivation(trait);
                _synergyActivations[trait] = activation;

                SynergyController controller = SynergyControllerFactory.Instance.Create(activation);
                if (controller == null)
                {
                    Debug.LogError($"[SynergyManager] SummonTraitSynergyController 생성 실패: {trait.DisplayName}");
                    continue;
                }
                _controllers[trait] = controller;

                _synergyMembers[trait] = units;
            }

            // 외부 노출 readonly 뷰 재구성 — SummonerEffect 항목과 SummonTrait 항목을 합산하여 새 사전 생성
            _synergyMembersPublic = new Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>>(_synergyMembers.Count);
            foreach (KeyValuePair<SynergyDefinitionData, List<UnitLoadOutData>> kv in _synergyMembers)
            {
                _synergyMembersPublic[kv.Key] = kv.Value;
            }

            // OnSynergyRecalculatedEventDto 발행 안 함 — 본 메서드는 Start 시점에 동기 호출되며,
            // SynergyListPanel.Start(executionOrder 100) 가 이후 _synergyActivations 를 자체 조회하여
            // SummonTrait 활성화를 바인딩한다. 초기 통합 시점에 이벤트를 발행하면 SynergyListPanel 의
            // 인디케이터가 아직 바인딩되지 않은 상태에서 SortIndicators 가 BoundActivation 에 접근하여
            // NRE 발생. 신규 분배 결과의 카운트는 0 이므로 가시성 갱신도 필요 없음.
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
    }
}
