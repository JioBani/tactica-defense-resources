using System;
using System.Collections.Generic;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.Draggable;
using Common.Scripts.GlobalEventBus;
using Common.Data.Configs;
using Common.Scripts.BubbleMessage;
using Common.Scripts.Rxs;
using Common.Scripts.SceneSingleton;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Ui;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

// ─────────────────────────────────────────────
// MarketManager: 소환터미널의 경제 로직을 관리한다.
// 마나, 스캔(리롤), 스캔 잠금, 소환수 구매, 배치 상한 증가.
// UI 표시는 MarketUiManager가 담당한다.
// ─────────────────────────────────────────────
namespace Scenes.Battle.Feature.Markets
{
    public class MarketManager : SceneSingleton<MarketManager>, IStateListener<PhaseType>
    {
        [SerializeField] private GameObject market;
        [SerializeField] private DefenderSellZone sellZone;
        [SerializeField] private DefenderManager defenderManager;
        [SerializeField] private List<UnitLoadOutData> appearUnits;
        public RxValue<int> Mana = new RxValue<int>(0);
        [SerializeField] private ManaIncomeConfig manaIncomeConfig;
        [SerializeField] private PlacementConfig placementConfig;
        [SerializeField] private StarProbabilityConfig starProbabilityConfig;

        public readonly RxValue<int> DefenderPlacementLimit = new RxValue<int>(0);
        public readonly RxValue<int> Level = new RxValue<int>(1);
        public readonly RxValue<int> LevelUpMana = new RxValue<int>(0);
        public readonly RxValue<int> RerollMana = new RxValue<int>(2);

        /// <summary>스캔 잠금 상태. true이면 라운드 시작 시 자동 재스캔을 스킵한다.</summary>
        public readonly RxValue<bool> IsScanLocked = new RxValue<bool>(false);

        int _levelUpIndex;

        MarketUnitRoller _roller;
        public MarketUnitRoller Roller => _roller;

        public Action<List<MarketDefenderSlot>> OnSlotRerolled;
        public Action<OnManaNotEnoughDto> OnManaNotEnough;

        protected override void OnAwakeSingleton()
        {
            base.OnAwakeSingleton();

            _roller = new MarketUnitRoller(appearUnits, starProbabilityConfig);

            // TOOD: RoundManager 에 게임 시작 상태를 만들고 그곳에 콜백 등록
            if (placementConfig == null)
                throw new NullReferenceException("[MarketManager] PlacementConfig가 할당되지 않았습니다.");
            if (placementConfig.levelUpTable.Length == 0)
                throw new InvalidOperationException("[MarketManager] PlacementConfig.levelUpTable이 비어있습니다.");

            DefenderPlacementLimit.Value = placementConfig.initialPlacementLimit;
            _levelUpIndex = 0;
            LevelUpMana.Value = placementConfig.levelUpTable[0].cost;
        }

        private void OnEnable()
        {
            RoundManager.Instance.RegisterListener(this);
            GlobalEventBus.Subscribe<OnDefenderDragEventDto>(OnDefenderDrag);
        }

        private void OnDisable()
        {
            RoundManager.Instance.UnregisterListener(this);
            GlobalEventBus.Unsubscribe<OnDefenderDragEventDto>(OnDefenderDrag);
        }

        // IStateListener 명시적 구현
        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Maintenance)
            {
                OnRoundStart();
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

        private void OnRoundStart()
        {
            Mana.Value += GetRoundStartIncome();

            if (IsScanLocked.Value)
            {
                IsScanLocked.Value = false;
            }
            else
            {
                RerollSlots();
            }
        }

        private bool BuySomething(int mana, string notEnoughManaMessage = null)
        {
            if (mana > Mana.Value)
            {
                // TODO: UI에 표시
                OnManaNotEnough?.Invoke(new OnManaNotEnoughDto());
                if (notEnoughManaMessage != null)
                {
                    BubbleMessageSpawner.Instance.SpawnAtScreen(
                        "마나가 부족합니다.",
                        Vector2.zero,
                        new BubbleMessageParams(
                            fontSize: 20    
                        )
                    );
                }
                return false;
            }
            else
            {
                Mana.Value -= mana;
                return true;
            }
        }

        private void RerollSlots()
        {
            List<MarketDefenderSlot> slots = _roller.PickUnits(4, Level.Value);

            OnSlotRerolled?.Invoke(slots);
        }

        /// <summary>스캔 잠금 상태를 토글한다.</summary>
        public void ToggleScanLock()
        {
            IsScanLocked.Value = !IsScanLocked.Value;
        }

        public void Reroll()
        {
            if (BuySomething(RerollMana.Value, "마나가 부족합니다."))
            {
                RerollSlots();
            }
        }

        /// <summary>
        /// 마켓 슬롯의 소환수를 구매하여 대기석에 배치한다.
        /// </summary>
        public bool BuyDefender(MarketDefenderSlot slot)
        {
            if (BuySomething(slot.UnitLoadOutData.Unit.Cost, "마나가 부족합니다."))
            {
                defenderManager.GenerateDefender(slot.UnitLoadOutData, slot.Star);
                slot.MarkAsSold();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsDefenderLimitExceeded()
        {
            return defenderManager.GetPlacementCount(Placement.BattleArea) >= DefenderPlacementLimit.Value;
        }

        public bool IsMaxLevel()
        {
            return _levelUpIndex >= placementConfig.levelUpTable.Length;
        }

        public bool LevelUp()
        {
            if (IsMaxLevel()) return false;

            var entry = placementConfig.levelUpTable[_levelUpIndex];

            if (BuySomething(entry.cost, "마나가 부족합니다."))
            {
                DefenderPlacementLimit.Value = entry.placementLimit;
                _levelUpIndex++;
                LevelUpMana.Value = IsMaxLevel() ? 0 : placementConfig.levelUpTable[_levelUpIndex].cost;
                Level.Value += 1;

                return true;
            }

            return false;
        }

        private void OnDefenderDrag(OnDefenderDragEventDto dto)
        {
            if (dto.state == DragState.DragStart)
            {
                market.SetActive(false);
                sellZone.gameObject.SetActive(true);
            }
            else if(dto.state == DragState.DragEnd)
            {
                market.SetActive(true);
                sellZone.TrySell(dto.defender);
                sellZone.gameObject.SetActive(false);
            }
        }

        public void Sell(Defender defender)
        {
            defenderManager.RemoveDefender(defender);
            Mana.Value += defender.UnitLoadOutData.Unit.Cost;
        }

        private int GetRoundStartIncome()
        {
            int roundIndex = RoundManager.Instance.RoundIndex;
            int income = manaIncomeConfig.GetBaseMana(roundIndex);

            // 이자
            int steps = Mathf.Min(Mana.Value / manaIncomeConfig.manaPerInterestStep, manaIncomeConfig.maxInterest);
            income += steps * manaIncomeConfig.interestPerStep;

            // 연승 보너스
            // if (winStreak > 0 && winStreak < winStreakBonusByCount.Length)
            //     income += winStreakBonusByCount[winStreak];
            //
            // // 연패 보너스
            // if (loseStreak > 0 && loseStreak < loseStreakBonusByCount.Length)
            //     income += loseStreakBonusByCount[loseStreak];

            return income;
        }
    }
}
