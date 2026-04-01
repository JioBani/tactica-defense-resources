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

        /// <summary>모든 시너지 상태 목록. UI 표시 등 외부 조회용.</summary>
        public IReadOnlyDictionary<SynergyDefinitionData, SynergyActivation> SynergyActivations => _synergyActivations;

        // TODO: 디버그용. 시너지 UI 구현 후 제거한다.
        [Header("디버그 (런타임 확인용)")]
        [SerializeField] private SerializableDictionary<string, string> debugSynergyStatus = new();

        protected override void OnAwakeSingleton() { }

        private void Start()
        {
            IReadOnlyList<SummonerLoadOutData> summoners = SummonerManager.Instance.Summoners;

            // 소환수 → 시너지 역방향 맵 구축 (Defender 스폰 시 시너지 주입에 사용)
            BuildUnitSynergyMap(summoners);

            // 유니크 소환술사 효과 추출 → SynergyController 생성
            var uniqueEffects = new HashSet<SynergyDefinitionData>();
            foreach (SummonerLoadOutData summoner in summoners)
            {
                uniqueEffects.Add(summoner.Summoner.SummonerEffect);
            }

            InitializeSynergyActivations(uniqueEffects);
        }

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnDefenderChangedEventDto>(HandleDefenderChanged);

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

        /// <summary>Defender 스폰 시 역방향 맵에서 소환술사 효과를 조회하여 주입한다.</summary>
        private void HandleDefenderChanged(OnDefenderChangedEventDto dto)
        {
            if (dto.Change == DefenderChanges.Spawn)
            {
                if (_unitSynergyMap.TryGetValue(dto.Defender.UnitLoadOutData, out SynergyDefinitionData synergy))
                {
                    dto.Defender.AddSynergy(synergy);
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
