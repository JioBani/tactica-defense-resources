using System;
using System.Collections.Generic;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.Units.Attackers;
using Scenes.Battle.Feature.Units.Attackables;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.ActionStates
{
    public class ActionStateController : StateBaseController<ActionStateType>
    {
        [SerializeField] private Unit self;
        [SerializeField] private Attacker attacker;
        [SerializeField] private bool canMove;

        protected override ActionStateType CheckStateTransition(ActionStateType currentState)
        {
            // 우선순위 1: 체력이 0 이하면 무조건 Downed 상태로 전환
            if (self.StatSheet.Health <= 0)
            {
                return ActionStateType.Downed;
            }

            // 우선순위 2: 각 상태별 전환 조건 체크
            switch (currentState)
            {
                case ActionStateType.Idle:
                    // Idle -> Attack: 타겟이 있으면 공격
                    if (attacker.Victim)
                    {
                        return ActionStateType.Attack;
                    }
                    break;

                case ActionStateType.Move:
                    // Move -> Attack: 타겟이 있으면 공격
                    if (attacker.Victim)
                    {
                        return ActionStateType.Attack;
                    }
                    break;

                case ActionStateType.Attack:
                    // Attack -> Move: 타겟이 없거나 타겟이 Downed 상태면 이동
                    if (!attacker.Victim ||
                        attacker.Victim.Unit.ActionStateController.CurrentState == ActionStateType.Downed)
                    {
                        return canMove ? ActionStateType.Move : ActionStateType.Idle;
                    }
                    break;

                case ActionStateType.Downed:
                    // Downed 상태는 전환 없음
                    break;

                case ActionStateType.Freeze:
                    // Freeze 상태는 전환 없음
                    break;
            }

            return currentState;
        }

        private void OnEnable()
        {
            GlobalEventBus.Subscribe<OnBattleLoseEventDto>(OnBattleEnd);
            GlobalEventBus.Subscribe<OnBattleWinEventDto>(OnBattleEnd);
            StartStateBase(canMove ? ActionStateType.Move :  ActionStateType.Idle);
        }

        private void OnDisable()
        {
            GlobalEventBus.Unsubscribe<OnBattleLoseEventDto>(OnBattleEnd);
            GlobalEventBus.Unsubscribe<OnBattleWinEventDto>(OnBattleEnd);
        }

        private void OnBattleEnd<T>(T _) where T : struct
        {
            RequestStateChange(ActionStateType.Freeze);
        }
    }
}