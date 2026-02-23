// ─────────────────────────────────────────────
// DefenderFusionManager: 소환수 합성(승급) 및 강화 합성 시스템.
// 정비 페이즈에서만 합성을 실행하고, 전투 중에는 불가 안내를 표시한다.
// 기본 합성(동일 3개→승급)과 강화 합성(3성+2성→강화)을 연쇄 처리한다.
// ─────────────────────────────────────────────
using System.Collections.Generic;
using System.Linq;
using Common.Scripts.BubbleMessage;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Fusion
{
    public class DefenderFusionManager : MonoBehaviour, IStateListener<PhaseType>
    {
        [SerializeField] private DefenderManager defenderManager;

        private readonly FusionGroupDetector _detector = new FusionGroupDetector();
        private readonly EnhancementFusionDetector _enhancementDetector = new EnhancementFusionDetector();

        /// <summary>기본 합성이 허용되는 최대 성급 (미만). 3이면 1성, 2성만 기본 합성 가능.</summary>
        private const int MaxBasicFusionStar = 3;

        private bool _isMaintenancePhase;

        private void OnEnable()
        {
            RoundManager.Instance.RegisterListener(this);
            defenderManager.OnDefenderChange += OnDefenderChange;
        }

        private void OnDisable()
        {
            RoundManager.Instance.UnregisterListener(this);
            defenderManager.OnDefenderChange -= OnDefenderChange;
        }

        // ── IStateListener 명시적 구현 ──

        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Maintenance)
            {
                _isMaintenancePhase = true;
                TryExecuteFusion();
            }
            else
            {
                _isMaintenancePhase = false;
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType) { }
        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType) { }

        /// <summary>디펜더 생성/제거 시 합성 조건을 확인한다.</summary>
        private void OnDefenderChange(Defender defender, DefenderChanges change)
        {
            if (change != DefenderChanges.Spawn) return;

            if (_isMaintenancePhase)
            {
                TryExecuteFusion();
            }
            else
            {
                // 전투 중 합성 조건 충족 시 불가 안내
                var candidates = BuildCandidates();
                if (_detector.HasFusionGroup(candidates, MaxBasicFusionStar)
                    || _enhancementDetector.HasEnhancementPair(candidates))
                {
                    BubbleMessageSpawner.Instance.SpawnAtWorld(
                        "전투 중에는 합성이 불가능합니다",
                        defender.transform.position
                    );
                }
            }
        }

        /// <summary>기본 합성과 강화 합성을 연쇄 실행한다. 둘 다 불가능할 때까지 반복한다.</summary>
        private void TryExecuteFusion()
        {
            while (true)
            {
                var candidates = BuildCandidates();

                // 기본 합성 우선 탐지 (maxStar 제한)
                var groupIndices = _detector.FindFusionGroup(candidates, MaxBasicFusionStar);
                if (groupIndices != null)
                {
                    ExecuteFusion(groupIndices);
                    continue;
                }

                // 강화 합성 탐지
                var enhancementResult = _enhancementDetector.FindEnhancementPair(candidates);
                if (enhancementResult != null)
                {
                    ExecuteEnhancement(enhancementResult.Value);
                    continue;
                }

                break;
            }
        }

        /// <summary>기본 합성 1회 실행: 그룹 내에서 승급 대상을 실시간 위치로 선정하고, 나머지 제거.</summary>
        private void ExecuteFusion(int[] groupIndices)
        {
            var defenders = defenderManager.Defenders;

            // 실시간 transform.position으로 승급 대상 선정: Y 내림차순(위쪽) → X 오름차순(왼쪽)
            var sorted = groupIndices
                .OrderByDescending(i => defenders[i].transform.position.y)
                .ThenBy(i => defenders[i].transform.position.x)
                .ToList();

            Defender survivor = defenders[sorted[0]];
            var consumed = new List<Defender> { defenders[sorted[1]], defenders[sorted[2]] };

            foreach (var material in consumed)
            {
                defenderManager.RemoveDefender(material);
            }

            // 승급
            survivor.StatSheet.UpgradeStar();
            int newStar = survivor.StatSheet.Star;

            // 합성 완료 이벤트 발행
            GlobalEventBus.Publish(new OnDefenderFusedEventDto(survivor, newStar));

            // 버블 메시지로 합성 결과 표시
            BubbleMessageSpawner.Instance.SpawnAtWorld(
                $"★{newStar} 승급!",
                survivor.transform.position
            );
        }

        /// <summary>강화 합성 1회 실행: 2성 재료를 소모하고 3성+ 타겟을 강화한다.</summary>
        private void ExecuteEnhancement(EnhancementFusionResult result)
        {
            var defenders = defenderManager.Defenders;
            Defender target = defenders[result.TargetIndex];
            Defender material = defenders[result.MaterialIndex];

            // 재료 제거
            defenderManager.RemoveDefender(material);

            // 강화
            target.StatSheet.Reinforce();
            int star = target.StatSheet.Star;
            int reinforcement = target.StatSheet.Reinforcement;

            // 합성 완료 이벤트 발행
            GlobalEventBus.Publish(new OnDefenderFusedEventDto(target, star));

            // 버블 메시지로 강화 결과 표시
            BubbleMessageSpawner.Instance.SpawnAtWorld(
                $"★{star}+{reinforcement}강 강화!",
                target.transform.position
            );
        }

        /// <summary>현재 디펜더 목록에서 FusionCandidate 리스트를 생성한다.</summary>
        private List<FusionCandidate> BuildCandidates()
        {
            var defenders = defenderManager.Defenders;
            var candidates = new List<FusionCandidate>(defenders.Count);

            for (int i = 0; i < defenders.Count; i++)
            {
                var defender = defenders[i];
                candidates.Add(new FusionCandidate(
                    unitDefinitionId: defender.UnitLoadOutData.Unit.ID,
                    star: defender.StatSheet.Star,
                    index: i
                ));
            }

            return candidates;
        }
    }
}
