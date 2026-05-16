using System;
using System.Threading;
using Common.Data.Battlefields;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.Rxs;
using Common.Scripts.SceneDataManager;
using Common.Scripts.StateBase;
using Cysharp.Threading.Tasks;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Rounds
{

    public class RoundManager : StateBaseController<PhaseType>, IStateListener<PhaseType>
    {
        static RoundManager _instance;
        static bool _quitting;
        public int RoundIndex { get; private set; } = 0;
        [SerializeField] private RoundAggressorManager roundAggressorManager;
        [SerializeField] private DefenderManager defenderManager;

        [Header("Phase Transition Times")]
        [SerializeField] private float readyPhaseDuration = 3f;
        [SerializeField] private float endPhaseDuration = 3f;

        public const int MaxFailCount = 2;

        public RxValue<int> RoundFailCount { get; private set; } = new RxValue<int>(0);

        public int RemainingFailCount => MaxFailCount - RoundFailCount.Value;

        public bool IsMissionFailed => RoundFailCount.Value >= MaxFailCount;

        private CancellationTokenSource _endPhaseCts;
        private CancellationTokenSource _readyPhaseCts;
        private CancellationTokenSource _roundLoseCts;

        private bool _isRetry;

        public static RoundManager Instance
        {
            get
            {
                if (_quitting) return null;

                // 이미 캐시되어 있으면 반환
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<RoundManager>(FindObjectsInactive.Exclude);

                return _instance;
            }
        }

        /// <summary>
        /// 현재 전장 데이터 (SceneDataManager에서 가져옴)
        /// </summary>
        public BattlefieldData Battlefield => SceneDataManager.Instance.selectedBattlefield.Get();

        protected override void Awake()
        {
            base.Awake();

            DebugMode = true;

            // IStateListener 등록 (자기 자신)
            RegisterListener(this);
        }

        private void Start()
        {
            // TODO: 전장 초기화 로직(편성 주입·씬 데이터 로드 등)이 추가될 경우 발행 시점을 해당 초기화 완료 이후로 이동 필요
            GlobalEventBus.Publish(new OnBattleStartEventDto());
            StartRound();
        }

        private void OnEnable()
        {
            RoundFailCount.Value = 0;
            
            // 침략자 모두 처치 이벤트 구독
            GlobalEventBus.Subscribe<RoundAggressorCompletedEventDto>(OnAggressorAllCompleted);

            // 라운드 실패 이벤트 구독
            GlobalEventBus.Subscribe<OnRoundLoseEventDto>(OnRoundLose);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<RoundAggressorCompletedEventDto>(OnAggressorAllCompleted);
            GlobalEventBus.Unsubscribe<OnRoundLoseEventDto>(OnRoundLose);
            _endPhaseCts?.Cancel();
            _endPhaseCts?.Dispose();
            _readyPhaseCts?.Cancel();
            _readyPhaseCts?.Dispose();
            _roundLoseCts?.Cancel();
            _roundLoseCts?.Dispose();
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            switch (phaseType)
            {
                case PhaseType.Maintenance:
                    if (_isRetry)
                    {
                        _isRetry = false;
                    }
                    else
                    {
                        IncrementRoundIndex();
                    }
                    break;

                case PhaseType.Ready:
                    WaitAndTransitionToCombat().Forget();
                    break;

                case PhaseType.End:
                    WaitAndTransitionToMaintenance().Forget();
                    break;

                case PhaseType.RoundLose:
                    RoundFailCount.Value++;
                    _isRetry = true;
                    WaitAndTransitionAfterRoundLose().Forget();
                    break;

                case PhaseType.BattleWin:
                    GlobalEventBus.Publish(new OnBattleWinEventDto());
                    break;

                case PhaseType.BattleLose:
                    GlobalEventBus.Publish(new OnBattleLoseEventDto());
                    break;
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType)
        {
            // Run 단계에서는 특별한 동작 없음
        }

        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType)
        {
            // Exit 단계에서는 특별한 동작 없음
        }

        protected override PhaseType CheckStateTransition(PhaseType currentPhase)
        {
            // 각 Phase별 전환 조건 체크
            switch (currentPhase)
            {
                case PhaseType.Maintenance:
                    // Maintenance는 SetReady() 호출로만 전환
                    break;

                case PhaseType.Ready:
                    // Ready -> Combat: 3초 후 자동 전환 (UniTask로 처리)
                    break;

                case PhaseType.Combat:
                    break;

                case PhaseType.RoundLose:
                    // RoundLose는 UniTask로 자동 전환
                    break;

                case PhaseType.BattleLose:
                    // BattleLose는 전환 없음
                    break;

                case PhaseType.End:
                    // End는 3초 후 자동 전환 (UniTask로 처리)
                    break;
            }

            return currentPhase;
        }

        public void StartRound()
        {
            StartStateBase(PhaseType.Maintenance);
        }

        public RoundData GetCurrentRoundData()
        {
            return Battlefield.Rounds[RoundIndex];
        }

        public void SetReady()
        {
            if (CurrentState == PhaseType.Maintenance)
            {
                RequestStateChange(PhaseType.Ready);
            }
        }

        public void IncrementRoundIndex()
        {
            RoundIndex++;
        }

        // 라운드의 모든 침략자가 처치되었을 때
        private void OnAggressorAllCompleted(RoundAggressorCompletedEventDto _)
        {
            // 전투 페이즈면 종료 페이즈로 전환
            if (CurrentState == PhaseType.Combat)
            {
                RequestStateChange(PhaseType.End);
            }
        }

        // 침략자가 왼쪽 끝에 도달하여 라운드 실패
        private void OnRoundLose(OnRoundLoseEventDto _)
        {
            if (CurrentState == PhaseType.Combat)
            {
                RequestStateChange(PhaseType.RoundLose);
            }
        }

        /// <summary>
        /// Ready 페이즈 진입 시 3초 대기 후 Combat 페이즈로 전환
        /// </summary>
        private async UniTaskVoid WaitAndTransitionToCombat()
        {
            // 기존 취소 토큰 정리
            _readyPhaseCts?.Cancel();
            _readyPhaseCts?.Dispose();
            _readyPhaseCts = new CancellationTokenSource();

            try
            {
                // 설정된 시간만큼 대기
                await UniTask.Delay(TimeSpan.FromSeconds(readyPhaseDuration), cancellationToken: _readyPhaseCts.Token);

                // Combat 페이즈로 전환
                if (CurrentState == PhaseType.Ready)
                {
                    RequestStateChange(PhaseType.Combat);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 아무 작업도 하지 않음
            }
        }

        /// <summary>
        /// End 페이즈 진입 시 3초 대기 후 다음 페이즈로 전환
        /// 마지막 라운드면 BattleWin, 아니면 Maintenance
        /// </summary>
        private async UniTaskVoid WaitAndTransitionToMaintenance()
        {
            // 기존 취소 토큰 정리
            _endPhaseCts?.Cancel();
            _endPhaseCts?.Dispose();
            _endPhaseCts = new CancellationTokenSource();

            try
            {
                // 설정된 시간만큼 대기
                await UniTask.Delay(TimeSpan.FromSeconds(endPhaseDuration), cancellationToken: _endPhaseCts.Token);

                if (CurrentState == PhaseType.End)
                {
                    // 마지막 라운드면 BattleWin, 아니면 Maintenance
                    bool isLastRound = RoundIndex >= Battlefield.Rounds.Count - 1;
                    RequestStateChange(isLastRound ? PhaseType.BattleWin : PhaseType.Maintenance);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 아무 작업도 하지 않음
            }
        }

        /// <summary>
        /// RoundLose 페이즈 진입 시 대기 후 다음 페이즈로 전환
        /// 임무 실패(3회)면 BattleLose, 아니면 Maintenance(재도전)
        /// </summary>
        private async UniTaskVoid WaitAndTransitionAfterRoundLose()
        {
            _roundLoseCts?.Cancel();
            _roundLoseCts?.Dispose();
            _roundLoseCts = new CancellationTokenSource();

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(endPhaseDuration), cancellationToken: _roundLoseCts.Token);

                if (CurrentState == PhaseType.RoundLose)
                {
                    RequestStateChange(IsMissionFailed
                        ? PhaseType.BattleLose
                        : PhaseType.Maintenance);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 아무 작업도 하지 않음
            }
        }
    }
}
