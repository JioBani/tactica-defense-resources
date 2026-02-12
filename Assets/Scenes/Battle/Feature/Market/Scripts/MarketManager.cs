using System;
using System.Collections.Generic;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.Draggable;
using Common.Scripts.GlobalEventBus;
using Common.Data.Configs;
using Common.Scripts.Rxs;
using Common.Scripts.SceneSingleton;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Ui;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

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

        public readonly RxValue<int> DefenderPlacementLimit = new RxValue<int>(0);
        public readonly RxValue<int> Level = new RxValue<int>(1);
        public readonly RxValue<int> LevelUpMana = new RxValue<int>(5);
        public readonly RxValue<int> RerollMana = new RxValue<int>(2);

        MarketUnitRoller _roller;
        public MarketUnitRoller Roller => _roller;

        public Action<List<UnitLoadOutData>> OnSlotRerolled;
        public Action<OnManaNotEnoughDto> OnManaNotEnough;

        protected override void OnAwakeSingleton()
        {
            base.OnAwakeSingleton();

            _roller = new MarketUnitRoller(appearUnits);

            // IStateListener 등록

            // TOOD: RoundManager 에 게임 시작 상태를 만들고 그곳에 콜백 등록
            DefenderPlacementLimit.Value = placementConfig.initialPlacementLimit;
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
            RerollSlots();
        }

        private bool BuySomething(int mana, string notEnoughManaMessage = null)
        {
            if (mana > Mana.Value)
            {
                // TODO: UI에 표시
                OnManaNotEnough?.Invoke(new OnManaNotEnoughDto());
                if (notEnoughManaMessage != null)
                {
                    AlertManager.Instance.Alert(notEnoughManaMessage);
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
            List<UnitLoadOutData> units = _roller.PickUnits(4);

            OnSlotRerolled?.Invoke(units);
        }

        public void Reroll()
        {
            if (BuySomething(RerollMana.Value, "마나가 부족합니다."))
            {
                RerollSlots();
            }
        }

        public bool BuyDefender(UnitLoadOutData unit)
        {
            if (BuySomething(unit.Unit.Cost, "마나가 부족합니다."))
            {
                defenderManager.GenerateDefender(unit);
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

        public bool LevelUp()
        {
            if (BuySomething(LevelUpMana.Value, "마나가 부족합니다."))
            {
                Level.Value += 1;
                DefenderPlacementLimit.Value += 1; // 레벨업시 수호자 배치 상한 상승
                return true;
            }
            else
            {
                return false;
            }
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
