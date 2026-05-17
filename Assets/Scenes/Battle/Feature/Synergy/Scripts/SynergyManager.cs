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

        /// <summary>
        /// SummonTrait 배정 결과 (소환수 → SummonTrait 매핑). 전장 시작 시 채워지고 종료 시 초기화된다.
        /// 인스펙터에서 런타임 상태 확인 가능, 외부에서는 GetSummonTrait 로 조회.
        /// </summary>
        /// <remarks>
        /// TODO: SummonTrait 관련 동작(분배·통합·조회·라이프사이클 정리)과 데이터의 응집도가 높아지면
        ///       (예: SummonTrait 전용 로직이 늘어나 SynergyManager 의 본 책임과 분리되기 시작하면)
        ///       본 필드와 관련 메서드(DistributeAndIntegrateSummonTraits, GetSummonTrait,
        ///       OnBattleWin/Lose 핸들러의 SummonTrait 정리 부분, summonTraitMap 사용처)를
        ///       묶어 SummonTrait 전담 클래스로 분리한다. 분리 시 *데이터와 동작을 함께 묶어* 응집도를
        ///       유지한다 — 단순 데이터 홀더(Store)와 외부 서비스로 갈라놓는 패턴은 회피
        ///       (이전 SummonTraitStore + SummonTraitDistributor 분리 시 발생했던 스파게티 결합 회귀 방지).
        /// </remarks>
        [Header("SummonTrait 런타임 상태")]
        [SerializeField] private SerializableDictionary<UnitLoadOutData, SynergyDefinitionData> summonTraitMap = new();

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

        /// <summary>SummonTrait 자산 목록 보관 SO. 인스펙터에서 연결.</summary>
        [Header("SummonTrait")]
        [SerializeField] private SummonTraitRegistry summonTraitRegistry;

        /// <summary>SummonTrait 균등 랜덤 분배 로직 서비스.</summary>
        private readonly SummonTraitService _summonTraitService = new();

        // TODO: 디버그용. 시너지 UI 구현 후 제거한다.
        [Header("디버그 (런타임 확인용)")]
        [SerializeField] private SerializableDictionary<string, string> debugSynergyStatus = new();

        protected override void OnAwakeSingleton() { }

        private void Start()
        {
            IReadOnlyList<SummonerLoadOutData> summoners = SummonerManager.Instance.Summoners;

            // === SummonerEffect 초기화 ===

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

            // === SummonTrait 분배 + 통합 (SummonerEffect 와 동일 시점 처리) ===
            //
            // TODO: 전장 초기화 로직(편성 주입·씬 데이터 로드 등)이 본 호출보다 늦게 완료되어야 하는
            //       라이프사이클 의존성이 추가되면, RoundManager 같은 전장 라이프사이클 주관 클래스에서
            //       적절한 시점에 이벤트(OnBattleStartEventDto 류)를 발행하고 본 분배·통합 호출을
            //       해당 이벤트 구독 핸들러로 이동하여 명시적 라이프사이클 결합으로 전환한다.
            DistributeAndIntegrateSummonTraits(summoners);
        }

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
            GlobalEventBus.Subscribe<OnBattleWinEventDto>(OnBattleWin);
            GlobalEventBus.Subscribe<OnBattleLoseEventDto>(OnBattleLose);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
            GlobalEventBus.Unsubscribe<OnBattleWinEventDto>(OnBattleWin);
            GlobalEventBus.Unsubscribe<OnBattleLoseEventDto>(OnBattleLose);

            foreach (SynergyController controller in _controllers.Values)
            {
                controller.Dispose();
            }
        }

        /// <summary>전장 승리 시 SummonTrait 런타임 상태를 초기화한다.</summary>
        private void OnBattleWin(OnBattleWinEventDto _) => summonTraitMap.Clear();

        /// <summary>전장 패배 시 SummonTrait 런타임 상태를 초기화한다.</summary>
        private void OnBattleLose(OnBattleLoseEventDto _) => summonTraitMap.Clear();

        /// <summary>지정 소환수의 SummonTrait 배정 결과를 반환한다. 미배정 시 null.</summary>
        public SynergyDefinitionData GetSummonTrait(UnitLoadOutData unit)
        {
            if (unit == null)
            {
                Debug.LogError("[SynergyManager] GetSummonTrait: unit이 null입니다.");
                return null;
            }

            return summonTraitMap.TryGetValue(unit, out SynergyDefinitionData trait) ? trait : null;
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

                if (summonTraitMap.TryGetValue(unit, out SynergyDefinitionData summonTrait))
                {
                    dto.Defender.AddSynergy(summonTrait);
                }
            }
        }

        /// <summary>
        /// SummonTrait 분배를 실행하고 결과를 런타임 저장소·활성화·역참조 인덱스에 통합한다.
        /// SummonerEffect 초기화와 동일 시점에 호출되어 트리거 대칭을 유지한다.
        /// </summary>
        private void DistributeAndIntegrateSummonTraits(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            if (summonTraitRegistry == null)
            {
                Debug.LogError("[SynergyManager] summonTraitRegistry 미연결 — SummonTrait 분배 스킵");
                return;
            }

            // 분배 실행
            Dictionary<UnitLoadOutData, SynergyDefinitionData> traitMap =
                _summonTraitService.Distribute(summoners, summonTraitRegistry.Traits);

            // 런타임 상태에 저장 (전장 종료 이벤트로 OnBattleWin/Lose 핸들러가 초기화)
            summonTraitMap.Clear();
            foreach (KeyValuePair<UnitLoadOutData, SynergyDefinitionData> kv in traitMap)
            {
                summonTraitMap[kv.Key] = kv.Value;
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
