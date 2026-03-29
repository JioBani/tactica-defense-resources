using Common.Data.Summoners.SummonerDefinitions;
using Common.Data.Summoners.SummonerLoadOuts;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events.RoundEvents;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Summoners
{
    /// <summary>레인 끝에 고정 배치되는 소환술사 유닛.</summary>
    public class Summoner : Units.Unit, IStateListener<ActionStateType>
    {
        /// <summary>소환술사 정의 데이터. OnSpawn 이후 접근 가능.</summary>
        public SummonerDefinitionData SummonerDefinition { get; private set; }

        private void Awake()
        {
            ActionStateController.RegisterListener(this);
        }

        private void OnDestroy()
        {
            ActionStateController.UnregisterListener(this);
        }

        protected override void OnSpawn(UnitLoadOutData unitLoadOutData)
        {
            // SummonerLoadOutData로 캐스팅하여 소환술사 고유 데이터를 보관한다.
            if (unitLoadOutData is SummonerLoadOutData summonerLoadOutData)
            {
                SummonerDefinition = summonerLoadOutData.Summoner;
            }
            else
            {
                Debug.LogError($"[Summoner] UnitLoadOutData가 SummonerLoadOutData 타입이 아닙니다: {unitLoadOutData.name}");
            }
        }

        // ── IStateListener<ActionStateType> ──

        /// <summary>소환술사가 다운되면 호출된다.</summary>
        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Downed)
            {
                GlobalEventBus.Publish(new OnRoundLoseEventDto());
            }
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType) { }

        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType) { }
    }
}
