using System.Collections.Generic;
using System.Linq;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Units;
using Scenes.Battle.Feature.Units.ActionStates;
using Scenes.Battle.Feature.WaitingAreas;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Defenders
{
    public enum DefenderChanges
    {
        Spawn,
        Despawn,
    }
    
    public class DefenderManager : MonoBehaviour
    {
        [SerializeField] private UnitGenerator unitGenerator;
        private List<Defender> units = new List<Defender>();

        /// <summary>현재 존재하는 모든 디펜더 목록. 합성 탐지 등 외부 조회용.</summary>
        public IReadOnlyList<Defender> Defenders => units;

        public int GetPlacementCount(Placement placement)
        {
            return units.Count(defender => defender.Placement == placement);
        }

        /// <summary>
        /// 소환수를 생성하고 대기석에 배치한 뒤 관리 목록에 등록한다.
        /// </summary>
        /// <param name="unitLoadOutData">유닛 설정 데이터.</param>
        /// <param name="star">초기 성급. 마켓에서 결정된다.</param>
        public bool GenerateDefender(UnitLoadOutData unitLoadOutData, int star = 1)
        {
            //TODO: 대기석이 비어있지 않는지 확인하는 것을 여기서 하는것이 맞는지 고려 필요
            if (WaitingAreaReferences.Instance.waitingAreas.Find((zone) => zone.occupant == null))
            {
                var unit = unitGenerator.GenerateDefender(unitLoadOutData, star);
                
                unit.MoveToWaitingArea();

                Defender defender = unit.GetComponent<Defender>();
                
                units.Add(defender);
                
                GlobalEventBus.Publish(new OnDefenderChangedEventDto(defender, DefenderChanges.Spawn));

                return true;
            }
            else
            {
                Debug.Log("대기석이 가득찼습니다.");

                return false;
            }
        }

        public void RemoveDefender(Defender defender)
        {
            units.Remove(defender);
            GlobalEventBus.Publish(new OnDefenderChangedEventDto(defender, DefenderChanges.Despawn));
            unitGenerator.RemoveUnit(defender);
        }

        public bool IsAllDefenderDowned()
        {
            return units.All((unit) => unit.ActionStateController.CurrentState == ActionStateType.Downed);
        }

    }
}
