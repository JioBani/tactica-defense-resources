using System;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.Draggable;
using Common.Scripts.DynamicRepeater;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Rounds;
using Scenes.Battle.Feature.Rounds.Phases;
using Scenes.Battle.Feature.Unit.Skills.Contexts;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Defenders
{
    public class Defender : Units.Unit, IStateListener<PhaseType>
    {
        [SerializeField] private Draggable2D draggable;
        public Placement Placement { get; private set; }

        private void OnEnable()
        {
            draggable.OnDragStart += NotifyDragStart;
            draggable.OnDragEnd += NotifyDragEnd;
            RoundManager.Instance.RegisterListener(this);
        }

        private void OnDisable()
        {
            draggable.OnDragStart -= NotifyDragStart;
            draggable.OnDragEnd -= NotifyDragEnd;
            RoundManager.Instance.UnregisterListener(this);
        }

        // ── IStateListener<PhaseType> ──

        void IStateListener<PhaseType>.OnStateEnter(PhaseType phaseType)
        {
            if (phaseType == PhaseType.Maintenance)
            {
                // 라운드 종료 후 Maintenance 진입 시 HP를 전부 회복한다.
                StatSheet.RecoverFullHealth();
            }
        }

        void IStateListener<PhaseType>.OnStateRun(PhaseType phaseType) { }
        void IStateListener<PhaseType>.OnStateExit(PhaseType phaseType) { }

        //TODO: DefenerDragger 로 Dragger2D 상속해서 만드는게 나을듯
        private void NotifyDragStart()
        {
            GlobalEventBus.Publish(new OnDefenderDragEventDto(this, DragState.DragStart));
        }

        private void NotifyDragEnd()
        {
            GlobalEventBus.Publish(new OnDefenderDragEventDto(this, DragState.DragEnd));
        }

        public void OnDrop(Placement placement)
        {
            Placement = placement;
            GlobalEventBus.Publish(new OnDefenderPlacementChangedEventDto(this, placement));
        }
    }
}