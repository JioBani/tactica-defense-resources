using System;
using System.Collections.Generic;
using Common.Data.Synergies;
using Common.Data.Units.UnitLoadOuts;
using Common.Scripts.Draggable;
using Common.Scripts.DynamicRepeater;
using Common.Scripts.GlobalEventBus;
using Common.Scripts.StateBase;
using Scenes.Battle.Feature.Events;
using Scenes.Battle.Feature.Units.ActionStates;
using UnityEngine;

namespace Scenes.Battle.Feature.Unit.Defenders
{
    public class Defender : Units.Unit, IStateListener<ActionStateType>
    {
        [SerializeField] private Draggable2D draggable;
        public Placement Placement { get; private set; }

        private readonly List<SynergyDefinitionData> _synergies = new();

        /// <summary>런타임에 부여된 시너지 목록. 시너지 시스템이 조회한다.</summary>
        public IReadOnlyList<SynergyDefinitionData> Synergies => _synergies;

        /// <summary>시너지를 추가한다. 소환수 스폰 시 SynergyManager가 호출한다.</summary>
        public void AddSynergy(SynergyDefinitionData synergy)
        {
            if (synergy != null && !_synergies.Contains(synergy))
            {
                _synergies.Add(synergy);
            }
        }

        /// <summary>지정한 시너지를 보유하고 있는지 확인한다.</summary>
        public bool HasSynergy(SynergyDefinitionData definition)
        {
            return _synergies.Contains(definition);
        }

        private void OnEnable()
        {
            draggable.OnDragStart += NotifyDragStart;
            draggable.OnDragEnd += NotifyDragEnd;
            ActionStateController.RegisterListener(this);
        }

        private void OnDisable()
        {
            draggable.OnDragStart -= NotifyDragStart;
            draggable.OnDragEnd -= NotifyDragEnd;
            ActionStateController.UnregisterListener(this);
        }

        // ── IStateListener<ActionStateType> ──

        /// <summary>Waiting 진입 시 HP를 전부 회복한다.</summary>
        void IStateListener<ActionStateType>.OnStateEnter(ActionStateType stateType)
        {
            if (stateType == ActionStateType.Waiting)
            {
                StatSheet.RecoverFullHealth();
            }
        }

        void IStateListener<ActionStateType>.OnStateRun(ActionStateType stateType) { }
        void IStateListener<ActionStateType>.OnStateExit(ActionStateType stateType) { }

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