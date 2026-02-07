using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Data.Battlefields;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.RepeatX;
using Common.Scripts.StateBase;
using Common.Scripts.UniTaskHandles;
using Cysharp.Threading.Tasks;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RoundAggressorState
{
    Waiting,
    Spawning, // 스폰 중
    Spawned, // 스폰 완료
    Completed // 처치 완료
}

namespace Scenes.Battle.Feature.Rounds
{
    public class RoundAggressorManager : MonoBehaviour, IStateListener<PhaseType>
    {
        [SerializeField] private UnitGenerator unitGenerator;
        [SerializeField] private List<Transform> spawnPoints;

        // 한 라운드(Combat 페이즈) 동안의 스폰 작업을 제어할 취소 토큰 소스
        private CancellationTokenSource _roundContext;
        private readonly List<Units.Unit> _aggressors = new ();
        private readonly List<UniTaskHandle> _aggressorTaskHandles = new();

        public RoundAggressorState RoundAggressorState { get; private set; } = RoundAggressorState.Waiting;

        private void Awake()
        {
            // IStateListener 등록
            RoundManager.Instance.RegisterListener(this);
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Combat)
            {
                OnRoundEnter();
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Combat)
            {
                OnRoundEnd();
            }
        }

        private void OnDestroy()
        {
            RoundManager.Instance.UnregisterListener(this);
            CancelGeneration();
        }

        private void Update()
        {
            // 스폰 중일 때 모든 태스크가 완료되었는지 확인
            if (RoundAggressorState == RoundAggressorState.Spawning && IsAllAggressorsSpawned())
            {
                RoundAggressorState = RoundAggressorState.Spawned;
            }

            // 스폰 완료 후 모든 침략자가 비활성화되었는지 확인
            if (
                RoundManager.Instance.CurrentState == PhaseType.Combat &&
                RoundAggressorState == RoundAggressorState.Spawned &&
                _aggressors.All(aggressor => !aggressor.gameObject.activeInHierarchy)
            )
            {
                RoundAggressorState = RoundAggressorState.Completed;
                
                GlobalEventBus.Publish<RoundAggressorCompletedEventDto>(new RoundAggressorCompletedEventDto());
            }
        }

        private void OnRoundEnter()
        {
            RoundAggressorState = RoundAggressorState.Spawning;
            GenerateAggressorByRound();
        }

        private void OnRoundEnd()
        {
            RoundAggressorState = RoundAggressorState.Waiting;
            
            _aggressors.Clear();
            CancelGeneration();
        }
        
        /// <summary>
        /// 현재 라운드의 스폰 엔트리들을 순회하며 각각의 스폰 예약을 건다.
        /// fire-and-forget 형태로 예약만 걸고 즉시 반환한다.
        /// </summary>
        private void GenerateAggressorByRound()
        {
            _aggressorTaskHandles.Clear();
            // 이전 라운드의 토큰이 남아있을 수 있으므로 새 컨텍스트 생성
            _roundContext = new CancellationTokenSource();
            
            // 현재 라운드 정보 조회 (각 엔트리에는 지연시간, 수량, 유닛 로드아웃 포함)
            RoundData roundData = RoundManager.Instance.GetCurrentRoundData();

            // 각 스폰 엔트리별로 비동기 예약 실행 (완료를 기다리지 않음)
            foreach (var entry in roundData.spawnEntries)
            {
                var handle = GenerateAggressors(entry).ToHandle(); // ← 한 번만 변환
                _aggressorTaskHandles.Add(handle);
            }
        }

        private const int MaxSpawnCountPerEntry = 4;

        /// <summary>
        /// 단일 스폰 엔트리를 처리한다.
        /// 1) 지정된 지연시간만큼 대기
        /// 2) 취소되었는지 확인
        /// 3) count 만큼 유닛 생성 (최대 4명까지)
        /// </summary>
        private async UniTask GenerateAggressors(SpawnEntry entry)
        {
            try
            {
                // 라운드 공용 취소 토큰 (Exit 등 트리거 시 취소됨)
                var token = _roundContext.Token;

                await UniTask.Delay(entry.spawnTime.ToTimeSpan(), cancellationToken: token);

                // 대기 중 취소되었으면 바로 종료
                if (token.IsCancellationRequested) return;

                // 최대 4명까지만 등장
                int spawnCount = Math.Min(entry.count, MaxSpawnCountPerEntry);

                // 스폰포인트를 셔플하여 겹치지 않게 분배
                var shuffledSpawnPoints = GetShuffledSpawnPointIndices(spawnCount);

                for (int i = 0; i < spawnCount; i++)
                {
                    GenerateAggressor(entry.aggressor, shuffledSpawnPoints[i]);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소는 정상 흐름이므로 별도 로그 없이 무시
            }
        }

        /// <summary>
        /// 스폰포인트 인덱스를 셔플하여 겹치지 않게 반환
        /// </summary>
        private List<int> GetShuffledSpawnPointIndices(int count)
        {
            var indices = Enumerable.Range(0, spawnPoints.Count).ToList();

            // Fisher-Yates 셔플
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            // 필요한 개수만큼 반환 (스폰포인트 수보다 많으면 전체 반환)
            return indices.Take(Math.Min(count, indices.Count)).ToList();
        }

        private void GenerateAggressor(UnitLoadOutData unitLoadOutData, int spawnPointIndex)
        {
            Units.Unit unit = unitGenerator.GenerateAggressor(unitLoadOutData);

            unit.transform.position = new Vector3(
                spawnPoints[spawnPointIndex].position.x,
                spawnPoints[spawnPointIndex].position.y,
                unit.transform.position.z
            );

            _aggressors.Add(unit);
        }

        /// <summary>
        /// 라운드 종료 등 트리거 시 현재 진행 중인 모든 스폰 예약을 취소하고 자원 정리
        /// </summary>
        private void CancelGeneration()
        {
            _roundContext?.Cancel();   // 진행 중(대기 중) 작업들 일괄 취소 신호
            _roundContext?.Dispose();  // 토큰 소스 자원 해제
            _roundContext = null;
        }
        
        /// <summary>
        /// 모든 침략자가 소환 되었고, downed 인지 확인
        /// </summary>
        /// <returns></returns>
        public bool IsAllAggressorsCompleted()
        {
            return _aggressors.All(unit => unit.ActionStateController?.CurrentState == ActionStateType.Downed) &&
                   _aggressorTaskHandles.All(task => task.IsCompleted);
        }

        private bool IsAllAggressorsSpawned()
        {
            return _aggressorTaskHandles.All(task => task.IsCompleted);
        }
    }
}
