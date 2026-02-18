using Common.Scripts.BubbleMessage;
using Common.Scripts.Draggable;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Defenders;
using UnityEngine;

namespace Scenes.Battle.Feature.Rounds.WaitingArea
{
    [RequireComponent(typeof(DropZone2D))]
    public class WaitingArea : MonoBehaviour, IDropRule
    {
        [SerializeField] private Draggable2D draggable;

        private GameObject occupantUnit;

        private DropZone2D dropZone;
        private Defender _defender;
        
        private void Awake()
        {
            dropZone = GetComponent<DropZone2D>();
            
            dropZone.AddRule(this);
        }

        public bool CanAccept(Draggable2D draggable, DropZone2D before, DropZone2D after)
        {
            if (RoundManager.Instance.CurrentState != PhaseType.Maintenance)
            {
                BubbleMessageSpawner.Instance.SpawnAtWorld(
                    "전투 중에는 이동할 수 없습니다!",
                    draggable.transform.position
                );
                return false;
            }
            return true;
        }

        public void OnDropped(Draggable2D _draggable, DropZone2D before, DropZone2D after)
        {
            occupantUnit = _draggable.gameObject; //TODO: 이후 Unit 으로 변경
            _defender = occupantUnit.GetComponent<Defender>();
            _defender.OnDrop(Placement.WaitingArea);
        }

        public void OnDragOut(Draggable2D item, DropZone2D zone)
        {
            occupantUnit = null;
        }
    }
}


