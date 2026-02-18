using System;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.Draggable;
using Common.Scripts.DynamicRepeater;
using Common.Scripts.GlobalEventBus;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Unit.Skills.Contexts;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Defenders
{
    public class Defender : Units.Unit
    {
        [SerializeField] private Draggable2D draggable;
        public Placement Placement { get; private set; }

        private void OnEnable()
        {
            draggable.OnDragStart += NotifyDragStart;
            draggable.OnDragEnd += NotifyDragEnd;
        }

        private void OnDisable()
        {
            draggable.OnDragStart -= NotifyDragStart;
            draggable.OnDragEnd -= NotifyDragEnd;
        }

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