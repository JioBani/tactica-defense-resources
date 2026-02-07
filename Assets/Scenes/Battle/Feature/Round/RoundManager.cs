using System;
using System.Threading;
using Common.Data.Battlefields;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.SceneDataManager;
using Common.Scripts.StateBase;
using Cysharp.Threading.Tasks;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.LifeCrystals;
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
        [SerializeField] private LifeCrystalManager lifeCrystalManager;

        [Header("Phase Transition Times")]
        [SerializeField] private float readyPhaseDuration = 3f;
        [SerializeField] private float endPhaseDuration = 3f;

        private CancellationTokenSource _endPhaseCts;
        private CancellationTokenSource _readyPhaseCts;
        
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
            StartRound();
        }

        private void OnEnable()
        {
            // 침략자 모두 처치 이벤트 구독
            GlobalEventBus.Subscribe<RoundAggressorCompletedEventDto>(OnAggressorAllCompleted);
            
            // 생명수정 파괴 이벤트 구독
            GlobalEventBus.Subscribe<OnLifeCrystalDestroyEventDto>(OnLifeCrystalDestroy);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<RoundAggressorCompletedEventDto>(OnAggressorAllCompleted);
            GlobalEventBus.Unsubscribe<OnLifeCrystalDestroyEventDto>(OnLifeCrystalDestroy);
            _endPhaseCts?.Cancel();
            _endPhaseCts?.Dispose();
            _readyPhaseCts?.Cancel();
            _readyPhaseCts?.Dispose();
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            switch (phaseType)
            {
                case PhaseType.Maintenance:
                    IncrementRoundIndex();
                    break;

                case PhaseType.Ready:
                    WaitAndTransitionToCombat().Forget();
                    break;

                case PhaseType.End:
                    WaitAndTransitionToMaintenance().Forget();
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
            // 우선순위 2: 각 Phase별 전환 조건 체크
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

        // 생명수정 파괴시 패배 페이즈 전환
        private void OnLifeCrystalDestroy(OnLifeCrystalDestroyEventDto _)
        {
            RequestStateChange(PhaseType.BattleLose);
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
    }
}


