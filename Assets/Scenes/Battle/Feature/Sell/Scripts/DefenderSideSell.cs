using System;
using Common.Scripts.BubbleMessage;
using Common.Scripts.Draggable;
using Common.Scripts.InspectorDescriptionAttributes;
using Scenes.Battle.Feature.Markets;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Sells
{
    
    [InspectorDescription("정비 페이즈에서만 수호자 배치 가능하도록 룰 추가한 확장 Sell",InspectorMessageType.Info)]
    
    public class DefenderSideSell : ExclusiveDropZone2D, IDropRule
    {
        private Defender _defender;
        
        void Awake()
        {
            AddRule(this);
        }

        public bool CanAccept(Draggable2D draggable, DropZone2D before, DropZone2D after)
        {
            if (RoundManager.Instance.CurrentState != PhaseType.Maintenance)
            {
                Debug.Log("수호자는 준비 상태에서만 배치 할 수 있습니다.");
                return false;
            }

            if (!draggable.TryGetComponent<Defender>(out var defender))
            {
                Debug.Log("Is not defender.");
                return false;
            }

            if (MarketManager.Instance.IsDefenderLimitExceeded())
            {
                BubbleMessageSpawner.Instance.SpawnAtWorld(
                    "배치 한계 초과!",
                    draggable.transform.position
                );
                return false;
            }

            return true;
        }

        public void OnDropped(Draggable2D draggable, DropZone2D before, DropZone2D after)
        {
            _defender = draggable.GetComponent<Defender>();
            _defender.OnDrop(Placement.BattleArea);
        }

        public void OnDragOut(Draggable2D item, DropZone2D zone)
        {
        }
    }
}
