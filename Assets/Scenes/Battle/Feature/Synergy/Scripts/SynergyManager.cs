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

        /// <summary>소환수 → SummonerEffect 시너지 매핑. 편성 데이터에서 Start 시점에 결정적으로 구축.</summary>
        private readonly Dictionary<UnitLoadOutData, SynergyDefinitionData> _unitSummonerEffectMap = new();

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

            // 시너지 시스템 일괄 초기화. SummonerEffect·SummonTrait 두 종류를 동일 흐름으로 처리한다.
            //
            // TODO: 전장 초기화 로직(편성 주입·씬 데이터 로드 등)이 본 호출보다 늦게 완료되어야 하는
            //       라이프사이클 의존성이 추가되면, RoundManager 같은 전장 라이프사이클 주관 클래스에서
            //       적절한 시점에 이벤트(OnBattleStartEventDto 류)를 발행하고 본 초기화 호출을
            //       해당 이벤트 구독 핸들러로 이동하여 명시적 라이프사이클 결합으로 전환한다.
            InitializeAllSynergies(summoners);
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

        /// <summary>
        /// 시너지 시스템 일괄 초기화. SummonerEffect·SummonTrait 두 종류 시너지의 데이터 결정과 시스템 구축을
        /// 단일 메서드 내에서 순차 처리한다. 분리된 헬퍼 메서드를 두지 않는 이유는 멤버 변수를 통한
        /// 암묵적 의존성을 메서드 시그니처 밖으로 새지 않도록 하기 위함.
        ///
        /// 흐름:
        ///   Phase 1. 데이터 결정 — SummonerEffect 는 편성에서 결정적으로, SummonTrait 는 균등 랜덤 분배로
        ///            unit → synergy 매핑을 구축한다.
        ///   Phase 2. 시너지 → unit 역참조 인덱스 구축 — 두 종류 매핑을 합산한다.
        ///   Phase 3. 유니크 시너지마다 SynergyActivation 및 SynergyController 생성.
        ///   Phase 4. 외부 노출용 readonly 뷰 재구성.
        /// </summary>
        private void InitializeAllSynergies(IReadOnlyList<SummonerLoadOutData> summoners)
        {
            // ── Phase 1: 데이터 결정 ──

            // SummonerEffect: 편성에서 결정적으로 unit → effect 매핑 구축
            _unitSummonerEffectMap.Clear();
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
                    _unitSummonerEffectMap[unit] = effect;
                }
            }

            // SummonTrait: 균등 랜덤 분배로 unit → trait 매핑 구축
            summonTraitMap.Clear();
            if (summonTraitRegistry == null)
            {
                Debug.LogError("[SynergyManager] summonTraitRegistry 미연결 — SummonTrait 분배 스킵");
            }
            else
            {
                Dictionary<UnitLoadOutData, SynergyDefinitionData> traitMap =
                    _summonTraitService.Distribute(summoners, summonTraitRegistry.Traits);
                foreach (KeyValuePair<UnitLoadOutData, SynergyDefinitionData> kv in traitMap)
                {
                    summonTraitMap[kv.Key] = kv.Value;
                }
            }

            // ── Phase 2: 시너지 → unit 역참조 인덱스 구축 (두 종류 합산) ──
            _synergyMembers.Clear();
            foreach (KeyValuePair<UnitLoadOutData, SynergyDefinitionData> kv in _unitSummonerEffectMap)
            {
                AddSynergyMember(kv.Value, kv.Key);
            }
            foreach (KeyValuePair<UnitLoadOutData, SynergyDefinitionData> kv in summonTraitMap)
            {
                AddSynergyMember(kv.Value, kv.Key);
            }

            // ── Phase 3: 유니크 시너지마다 Activation + Controller 생성 ──
            foreach (SynergyDefinitionData synergy in _synergyMembers.Keys)
            {
                if (_synergyActivations.ContainsKey(synergy))
                {
                    continue;
                }

                var activation = new SynergyActivation(synergy);
                _synergyActivations[synergy] = activation;
                _controllers[synergy] = SynergyControllerFactory.Instance.Create(activation);
                activation.ActiveTier.OnChange += _ => UpdateDebugStatus();
            }

            // ── Phase 4: 외부 노출 readonly 뷰 재구성 ──
            _synergyMembersPublic = new Dictionary<SynergyDefinitionData, IReadOnlyList<UnitLoadOutData>>(_synergyMembers.Count);
            foreach (KeyValuePair<SynergyDefinitionData, List<UnitLoadOutData>> kv in _synergyMembers)
            {
                _synergyMembersPublic[kv.Key] = kv.Value;
            }
        }

        /// <summary>시너지 → 보유 unit 역참조 인덱스에 한 항목을 추가한다. 동일 unit 중복은 자연 제거.</summary>
        private void AddSynergyMember(SynergyDefinitionData synergy, UnitLoadOutData unit)
        {
            if (synergy == null || unit == null)
            {
                return;
            }

            if (!_synergyMembers.TryGetValue(synergy, out List<UnitLoadOutData> list))
            {
                list = new List<UnitLoadOutData>();
                _synergyMembers[synergy] = list;
            }

            if (!list.Contains(unit))
            {
                list.Add(unit);
            }
        }

        // ── Defender Spawn 처리 ──

        /// <summary>Defender 스폰 시 역방향 맵에서 소환술사 효과와 SummonTrait를 조회하여 둘 다 주입한다.</summary>
        private void HandleDefenderChanged(OnDefenderChangedEventDto dto)
        {
            if (dto.Change == DefenderChanges.Spawn)
            {
                UnitLoadOutData unit = dto.Defender.UnitLoadOutData;

                if (_unitSummonerEffectMap.TryGetValue(unit, out SynergyDefinitionData summonerEffect))
                {
                    dto.Defender.AddSynergy(summonerEffect);
                }

                if (summonTraitMap.TryGetValue(unit, out SynergyDefinitionData summonTrait))
                {
                    dto.Defender.AddSynergy(summonTrait);
                }
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
